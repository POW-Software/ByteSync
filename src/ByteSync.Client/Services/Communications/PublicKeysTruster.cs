﻿using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.EventsHubs;

namespace ByteSync.Services.Communications;

public class PublicKeysTruster : IPublicKeysTruster
{
    private readonly IEnvironmentService _environmentService;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ITrustApiClient _trustApiClient;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly ILogger<PublicKeysTruster> _logger;

    public PublicKeysTruster(IEnvironmentService environmentService, ICloudSessionApiClient cloudSessionApiClient,
        ITrustApiClient trustApiClient, IPublicKeysManager publicKeysManager, ITrustProcessPublicKeysRepository trustPublicKeysRepository,
        INavigationEventsHub navigationEventsHub, ILogger<PublicKeysTruster> logger)
    {
        _environmentService = environmentService;
        _cloudSessionApiClient = cloudSessionApiClient;
        _trustApiClient = trustApiClient;
        _publicKeysManager = publicKeysManager;
        _trustProcessPublicKeysRepository = trustPublicKeysRepository;
        _navigationEventsHub = navigationEventsHub;
        _logger = logger;
    }
    
    public async Task<JoinSessionResult> TrustAllMembersPublicKeys(string sessionId)
    {
        return await DoTrustMembersPublicKeys(sessionId);
    }
    
    public async Task<List<string>?> TrustMissingMembersPublicKeys(string sessionId)
    {
        // var parameters = new GetCloudSessionMembersParameters(sessionId,
        //     _publicKeysManager.GetMyPublicKeyInfo(), StartTrustCheckModes.None);
        
        var membersClientInstanceIds = await _cloudSessionApiClient.GetMembersClientInstanceIds(sessionId);

        // sessionMemberFullIds.RemoveAll(i => i.Equals(_connectionManager.FullId));
        
        // var parameters = new TrustCheckParameters 
        // { 
        //     SessionId = sessionId, 
        //     PublicKeyInfo = _publicKeysManager.GetMyPublicKeyInfo(),
        //     MembersFullIdsToCheck = sessionMemberFullIds
        // };
        //
        // await _trustApiClient.StartTrustCheck(parameters);
        //
        // var sessionMembersClientInstanceIds = await
        //     _connectionManager.HubWrapper.GetCloudSessionMembersAndStartTrustCheck(parameters);

        var nonFullyTrustedMembersIds = new List<string>();
        foreach (var memberInstanceId in membersClientInstanceIds)
        {
            if (!await _trustProcessPublicKeysRepository.IsFullyTrusted(sessionId, memberInstanceId))
            {
                nonFullyTrustedMembersIds.Add(memberInstanceId);
            }
        }

        // Pour tous les membres non trustés, on demande un trust
        if (nonFullyTrustedMembersIds.Count > 0)
        {
            var joinSessionResult = await DoTrustMembersPublicKeys(sessionId, nonFullyTrustedMembersIds);
            if (!joinSessionResult.IsOK)
            {
                return null;
            }
        }

        return membersClientInstanceIds;
    }

    // Appelé lors du StartTrustCheck sur les membres de la session pour qu'ils fournissent leur PublicKeyCheckData au joiner
    public async Task OnPublicKeyCheckDataAskedAsync((string sessionId, string clientInstanceId, PublicKeyInfo publicKeyInfo) tuple)
    {
        var isTrusted = _publicKeysManager.IsTrusted(tuple.publicKeyInfo);

        var memberPublicKeyCheckData = _publicKeysManager.BuildMemberPublicKeyCheckData(tuple.publicKeyInfo, isTrusted);

        await _trustProcessPublicKeysRepository.StoreLocalPublicKeyCheckData(tuple.sessionId, tuple.clientInstanceId, memberPublicKeyCheckData);

        var parameters = new GiveMemberPublicKeyCheckDataParameters
        {
            SessionId = tuple.sessionId,
            ClientInstanceId = tuple.clientInstanceId,
            PublicKeyCheckData = memberPublicKeyCheckData
        };
        
        await _trustApiClient.GiveMemberPublicKeyCheckData(parameters);
    }

    public async Task OnTrustPublicKeyRequestedAsync(RequestTrustProcessParameters requestTrustProcessParameters)
    {
        // On contrôle le salt
        var myPublicKeyCheckData = await _trustProcessPublicKeysRepository.GetLocalPublicKeyCheckData(requestTrustProcessParameters.SessionId,
            requestTrustProcessParameters.JoinerClientInstanceId);

        if (myPublicKeyCheckData == null)
        {
            throw new Exception("key is null");
        }

        // joinerPublicKeyCheckData et givenPublicKeyCheckData sont différents, mais ils doivent avoir le même salt
        if (myPublicKeyCheckData.Salt.IsNullOrEmpty() 
            || !myPublicKeyCheckData.Salt.Equals(requestTrustProcessParameters.JoinerPublicKeyCheckData.Salt))
        {
            throw new ArgumentException("Salt is not the same, unable to proceed");
        }

        // On informe en local qu'il faut un trust
        var peerTrustProcessData = await _trustProcessPublicKeysRepository.ResetPeerTrustProcessData(
            requestTrustProcessParameters.SessionId, myPublicKeyCheckData.OtherPartyPublicKeyInfo!.ClientId);
        
        var trustDataParameters = new TrustDataParameters(1, 1, false, requestTrustProcessParameters.SessionId, peerTrustProcessData);
        _navigationEventsHub.RaiseTrustKeyDataRequested(requestTrustProcessParameters.JoinerPublicKeyCheckData, trustDataParameters);
    }

    public async Task OnPublicKeyValidationIsFinishedAsync(PublicKeyValidationParameters publicKeyValidationParameters)
    {
        if (publicKeyValidationParameters.OtherPartyClientInstanceId.Equals(_environmentService.ClientInstanceId))
        {
            await _trustProcessPublicKeysRepository.SetOtherPartyChecked(publicKeyValidationParameters.SessionId, 
                publicKeyValidationParameters);
        }
    }
    
    public async Task OnPublicKeyValidationFinished(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters, bool isValidated)
    {
        var parameters = new PublicKeyValidationParameters();

        parameters.IsValidated = isValidated;
        parameters.SessionId = trustDataParameters.SessionId;
        parameters.IssuerClientId = _publicKeysManager.GetMyPublicKeyInfo().ClientId;
        parameters.OtherPartyClientInstanceId = publicKeyCheckData.IssuerClientInstanceId;
        parameters.OtherPartyPublicKey = publicKeyCheckData.IssuerPublicKeyInfo.PublicKey;

        trustDataParameters.PeerTrustProcessData.SetMyPartyChecked(parameters.IsValidated);
        
        // await _trustProcessPublicKeysHolder.SetMyPartyChecked(parameters.SessionId, parameters.IsValidated);
        
        await _trustApiClient.InformPublicKeyValidationIsFinished(parameters);
    }
    
    public Task OnPublicKeyValidationCanceled(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters)
    {
        trustDataParameters.PeerTrustProcessData.SetMyPartyCancelled();
        //
        // await _trustProcessPublicKeysHolder.SetMyPartyCanceled(trustDataParameters.SessionId);

        return Task.CompletedTask;
    }

    private async Task<JoinSessionResult> DoTrustMembersPublicKeys(string sessionId, List<string>? memberIdsToCheck = null)
    {
        // On demande que les membres de la session nous fournissent leurs PublicKeyCheckData
        var joinSessionResult = await InitiateAndWaitForTrustCheck(sessionId, memberIdsToCheck);
        if (!joinSessionResult.IsOK)
        {
            return joinSessionResult;
        }
        
        // On détermine les clés à truster
        var keysToTrust = new List<PublicKeyCheckData>();
        foreach (var publicKeyCheckData in await _trustProcessPublicKeysRepository.GetReceivedPublicKeyCheckData(sessionId))
        {
            var isFullTrusted = publicKeyCheckData.IsTrustedByOtherParty && _publicKeysManager.IsTrusted(publicKeyCheckData);

            if (!isFullTrusted)
            {
                keysToTrust.Add(publicKeyCheckData);
            }
            else
            {
                await _trustProcessPublicKeysRepository.SetFullyTrusted(sessionId, publicKeyCheckData);
            }
        }

        var cpt = 0;
        foreach (var publicKeyCheckData in keysToTrust)
        {
            cpt += 1;

            var peerTrustProcessData = await _trustProcessPublicKeysRepository
                .ResetPeerTrustProcessData(sessionId, publicKeyCheckData.IssuerPublicKeyInfo.ClientId);

            var trustDataParameters = new TrustDataParameters(cpt, keysToTrust.Count, true, sessionId, peerTrustProcessData);
            _navigationEventsHub.RaiseTrustKeyDataRequested(publicKeyCheckData, trustDataParameters);

            var myPublicKeyCheckData = _publicKeysManager.BuildJoinerPublicKeyCheckData(publicKeyCheckData);

            var requestTrustProcessParameters = new RequestTrustProcessParameters(sessionId, myPublicKeyCheckData,
                publicKeyCheckData.IssuerClientInstanceId);
            await _trustApiClient.RequestTrustPublicKey(requestTrustProcessParameters);

            var isTrustSuccess = await peerTrustProcessData.WaitForPeerTrustProcessFinished();

            if (!isTrustSuccess)
            {
                LogProblem("Can not join the session because at least one the Session Member is not trusted");

                return JoinSessionResult.BuildFrom(JoinSessionStatuses.TrustCheckFailed);
            }
            else
            {
                await _trustProcessPublicKeysRepository.SetFullyTrusted(sessionId, publicKeyCheckData);
            }
        }

        return JoinSessionResult.BuildProcessingNormally();  
    }
    
    private async Task<JoinSessionResult> InitiateAndWaitForTrustCheck(string sessionId, List<string>? memberIdsToCheck = null)
    {
        if (memberIdsToCheck == null)
        {
            var sessionMemberInstanceIds = await _cloudSessionApiClient.GetMembersClientInstanceIds(sessionId);
            
            memberIdsToCheck = new List<string>(sessionMemberInstanceIds);
        }
        
        if (memberIdsToCheck.Count == 0)
        {
            LogProblem("Members list is empty");
            return JoinSessionResult.BuildFrom(JoinSessionStatuses.SessionNotFound);
        }
        
        // var parameters = new GetCloudSessionMembersParameters(sessionId, _publicKeysManager.GetMyPublicKeyInfo(), startTrustCheckMode);
        // parameters.MembersToCheck = memberIdsToCheck;
        
        var parameters = new TrustCheckParameters 
        { 
            SessionId = sessionId, 
            PublicKeyInfo = _publicKeysManager.GetMyPublicKeyInfo(),
            MembersInstanceIdsToCheck = memberIdsToCheck
        };

        await _trustProcessPublicKeysRepository.ResetJoinerTrustProcessData(sessionId);
        var result = await _trustApiClient.StartTrustCheck(parameters);

        // if (memberIdsToCheck != null)
        // {
        //     // On retire tous les membres qui ne seraient plus membres de la session
        //     memberIdsToCheck.RemoveAll(m => !sessionMemberFullIds.Contains(m));
        //     
        //     // Il est possible (bien que peu probable) que la liste soit vide, auquel cas, il n'y aurait pas de check à faire
        //     if (memberIdsToCheck.Count == 0)
        //     {
        //         // On considère alors que tout est checké
        //         return JoinSessionResult.BuildProcessingNormally();
        //     }
        // }

        if (result == null || !result.IsOK)
        {
            _logger.LogError("Can not start trust check");
            return JoinSessionResult.BuildFrom(JoinSessionStatuses.UnexpectedError);
        }
        
        await _trustProcessPublicKeysRepository.SetExpectedPublicKeyCheckDataCount(sessionId, result.MembersInstanceIds);

        var isWaitOK = await _trustProcessPublicKeysRepository
            .WaitAsync(sessionId, data => data.JoinerTrustProcessData.WaitForAllPublicKeyCheckDatasReceived,
            TimeSpan.FromSeconds(30));

        if (isWaitOK)
        {
            return JoinSessionResult.BuildProcessingNormally();
        }
        else
        {
            _logger.LogWarning("Timeout during trust check process");
            return JoinSessionResult.BuildFrom(JoinSessionStatuses.TrustCheckFailed);
        }
    }
    
    private void LogUnknownSessionReceived(string? sessionId, [CallerMemberName] string caller = "")
    {
        if (caller.IsNullOrEmpty())
        {
            caller = "UnknownCaller";
        }

        _logger.LogError("CloudSessionConnector.{caller}: unknown sessionId received ({sessionId})", caller, sessionId);
    }

    private void LogProblem(string problemDescription, [CallerMemberName] string caller = "")
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        _logger.LogWarning($"CloudSessionConnector.{caller}: {problemDescription}");
    }
    
    // private async Task WaitForEvent(Func<bool> waitFunction, string waitFailMessage)
    // {
    //     await Task.Run(() =>
    //     {
    //         bool isWaitOk = waitFunction.Invoke();
    //
    //         if (!isWaitOk)
    //         {
    //             _cloudSessionConnectionDataHolder.SetStatus(ConnectionStatuses.None);
    //             ClearConnectionData();
    //             
    //             throw new Exception(waitFailMessage);
    //         }
    //     });
    // }
}