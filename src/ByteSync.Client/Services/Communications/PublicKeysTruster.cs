using System.Runtime.CompilerServices;
using System.Threading;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;

namespace ByteSync.Services.Communications;

public class PublicKeysTruster : IPublicKeysTruster
{
    private readonly IEnvironmentService _environmentService;
    private readonly ITrustApiClient _trustApiClient;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly IDialogService _dialogService;
    private readonly IFlyoutElementViewModelFactory _flyoutElementViewModelFactory;
    private readonly ISessionMemberApiClient _sessionMemberApiClient;
    private readonly ILogger<PublicKeysTruster> _logger;
    
    public PublicKeysTruster(IEnvironmentService environmentService, ITrustApiClient trustApiClient,
        IPublicKeysManager publicKeysManager, ITrustProcessPublicKeysRepository trustPublicKeysRepository,
        IDialogService dialogService, IFlyoutElementViewModelFactory flyoutElementViewModelFactory,
        ISessionMemberApiClient sessionMemberApiClient, ILogger<PublicKeysTruster> logger)
    {
        _environmentService = environmentService;
        _trustApiClient = trustApiClient;
        _publicKeysManager = publicKeysManager;
        _trustProcessPublicKeysRepository = trustPublicKeysRepository;
        _dialogService = dialogService;
        _flyoutElementViewModelFactory = flyoutElementViewModelFactory;
        _sessionMemberApiClient = sessionMemberApiClient;
        _logger = logger;
    }
    
    public async Task<JoinSessionResult> TrustAllMembersPublicKeys(string sessionId, CancellationToken cancellationToken = default)
    {
        return await DoTrustMembersPublicKeys(sessionId, null, cancellationToken);
    }
    
    public async Task<List<string>?> TrustMissingMembersPublicKeys(string sessionId, CancellationToken cancellationToken = default)
    {
        var membersClientInstanceIds = await _sessionMemberApiClient.GetMembersClientInstanceIds(sessionId, cancellationToken);
        
        var nonFullyTrustedMembersIds = new List<string>();
        foreach (var memberInstanceId in membersClientInstanceIds)
        {
            if (!await _trustProcessPublicKeysRepository.IsFullyTrusted(sessionId, memberInstanceId))
            {
                nonFullyTrustedMembersIds.Add(memberInstanceId);
            }
        }
        
        // For all non-trusted members, a trust protocol is initiated
        if (nonFullyTrustedMembersIds.Count > 0)
        {
            var joinSessionResult = await DoTrustMembersPublicKeys(sessionId, nonFullyTrustedMembersIds, cancellationToken);
            if (!joinSessionResult.IsOK)
            {
                return null;
            }
        }
        
        return membersClientInstanceIds;
    }
    
    // Called during StartTrustCheck on the session members so that they provide their PublicKeyCheckData to the joiner
    public async Task OnPublicKeyCheckDataAskedAsync((string sessionId, string clientInstanceId, PublicKeyInfo publicKeyInfo) tuple)
    {
        // Validate protocol version compatibility before responding
        if (!ProtocolVersion.IsCompatible(tuple.publicKeyInfo.ProtocolVersion))
        {
            _logger.LogWarning(
                "Protocol version mismatch in OnPublicKeyCheckDataAskedAsync: joiner has version {JoinerVersion}, current version is {CurrentVersion}. Not responding to trust check request.",
                tuple.publicKeyInfo.ProtocolVersion, ProtocolVersion.CURRENT);
            
            return;
        }
        
        var isTrusted = _publicKeysManager.IsTrusted(tuple.publicKeyInfo);
        
        var memberPublicKeyCheckData = _publicKeysManager.BuildMemberPublicKeyCheckData(tuple.publicKeyInfo, isTrusted);
        
        await _trustProcessPublicKeysRepository.StoreLocalPublicKeyCheckData(tuple.sessionId, tuple.clientInstanceId,
            memberPublicKeyCheckData);
        
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
        // Check if the key is already stored
        var myPublicKeyCheckData = await _trustProcessPublicKeysRepository.GetLocalPublicKeyCheckData(
            requestTrustProcessParameters.SessionId,
            requestTrustProcessParameters.JoinerClientInstanceId);
        
        if (myPublicKeyCheckData == null)
        {
            throw new Exception("key is null");
        }
        
        // joinerPublicKeyCheckData and givenPublicKeyCheckData are different, but they must have the same salt
        if (myPublicKeyCheckData.Salt.IsNullOrEmpty()
            || !myPublicKeyCheckData.Salt.Equals(requestTrustProcessParameters.JoinerPublicKeyCheckData.Salt))
        {
            throw new ArgumentException("Salt is not the same, unable to proceed");
        }
        
        // Validate protocol version compatibility
        if (!ProtocolVersion.IsCompatible(requestTrustProcessParameters.JoinerPublicKeyCheckData.ProtocolVersion))
        {
            _logger.LogWarning("Protocol version mismatch: joiner has version {JoinerVersion}, current version is {CurrentVersion}",
                requestTrustProcessParameters.JoinerPublicKeyCheckData.ProtocolVersion, ProtocolVersion.CURRENT);
            
            throw new InvalidOperationException("Protocol version incompatible");
        }
        
        // Reset the peer trust process data
        var peerTrustProcessData = await _trustProcessPublicKeysRepository.ResetPeerTrustProcessData(
            requestTrustProcessParameters.SessionId, myPublicKeyCheckData.OtherPartyPublicKeyInfo!.ClientId);
        
        var trustDataParameters = new TrustDataParameters(1, 1, false, requestTrustProcessParameters.SessionId, peerTrustProcessData);
        var addTrustedClientViewModel = _flyoutElementViewModelFactory.BuilAddTrustedClientViewModel(
            requestTrustProcessParameters.JoinerPublicKeyCheckData, trustDataParameters);
        _dialogService.ShowFlyout(nameof(Resources.Shell_TrustedClients), false, addTrustedClientViewModel);
    }
    
    public async Task OnPublicKeyValidationIsFinishedAsync(PublicKeyValidationParameters publicKeyValidationParameters)
    {
        if (publicKeyValidationParameters.OtherPartyClientInstanceId.Equals(_environmentService.ClientInstanceId))
        {
            await _trustProcessPublicKeysRepository.SetOtherPartyChecked(publicKeyValidationParameters.SessionId,
                publicKeyValidationParameters);
        }
    }
    
    public async Task OnPublicKeyValidationFinished(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters,
        bool isValidated)
    {
        var parameters = new PublicKeyValidationParameters();
        
        parameters.IsValidated = isValidated;
        parameters.SessionId = trustDataParameters.SessionId;
        parameters.IssuerClientId = _publicKeysManager.GetMyPublicKeyInfo().ClientId;
        parameters.OtherPartyClientInstanceId = publicKeyCheckData.IssuerClientInstanceId;
        parameters.OtherPartyPublicKey = publicKeyCheckData.IssuerPublicKeyInfo.PublicKey;
        
        trustDataParameters.PeerTrustProcessData.SetMyPartyChecked(parameters.IsValidated);
        
        await _trustApiClient.InformPublicKeyValidationIsFinished(parameters);
    }
    
    public Task OnPublicKeyValidationCanceled(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters)
    {
        trustDataParameters.PeerTrustProcessData.SetMyPartyCancelled();
        
        return Task.CompletedTask;
    }
    
    private async Task<JoinSessionResult> DoTrustMembersPublicKeys(string sessionId, List<string>? memberIdsToCheck = null,
        CancellationToken cancellationToken = default)
    {
        // We ask the members of the session to provide us with their PublicKeyCheckData
        var joinSessionResult = await InitiateAndWaitForTrustCheck(sessionId, memberIdsToCheck, cancellationToken);
        if (!joinSessionResult.IsOK)
        {
            return joinSessionResult;
        }
        
        // Validate protocol version compatibility
        var receivedPublicKeyCheckData = await _trustProcessPublicKeysRepository.GetReceivedPublicKeyCheckData(sessionId);
        foreach (var publicKeyCheckData in receivedPublicKeyCheckData)
        {
            if (!ProtocolVersion.IsCompatible(publicKeyCheckData.ProtocolVersion))
            {
                _logger.LogWarning("Protocol version mismatch: member has version {MemberVersion}, current version is {CurrentVersion}",
                    publicKeyCheckData.ProtocolVersion, ProtocolVersion.CURRENT);
                
                return JoinSessionResult.BuildFrom(JoinSessionStatus.IncompatibleProtocolVersion);
            }
        }
        
        // We determine the keys to trust
        var keysToTrust = new List<PublicKeyCheckData>();
        foreach (var publicKeyCheckData in receivedPublicKeyCheckData)
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
            var addTrustedClientViewModel = _flyoutElementViewModelFactory.BuilAddTrustedClientViewModel(
                publicKeyCheckData, trustDataParameters);
            _dialogService.ShowFlyout(nameof(Resources.Shell_TrustedClients), false, addTrustedClientViewModel);
            
            var myPublicKeyCheckData = _publicKeysManager.BuildJoinerPublicKeyCheckData(publicKeyCheckData);
            
            var requestTrustProcessParameters = new RequestTrustProcessParameters(sessionId, myPublicKeyCheckData,
                publicKeyCheckData.IssuerClientInstanceId);
            await _trustApiClient.RequestTrustPublicKey(requestTrustProcessParameters, cancellationToken);
            
            var isTrustSuccess = await peerTrustProcessData.WaitForPeerTrustProcessFinished();
            
            if (!isTrustSuccess)
            {
                LogProblem("Can not join the session because at least one the Session Member is not trusted");
                
                return JoinSessionResult.BuildFrom(JoinSessionStatus.TrustCheckFailed);
            }
            else
            {
                await _trustProcessPublicKeysRepository.SetFullyTrusted(sessionId, publicKeyCheckData);
            }
        }
        
        return JoinSessionResult.BuildProcessingNormally();
    }
    
    private async Task<JoinSessionResult> InitiateAndWaitForTrustCheck(string sessionId, List<string>? memberIdsToCheck,
        CancellationToken cancellationToken)
    {
        if (memberIdsToCheck == null)
        {
            var sessionMemberInstanceIds = await _sessionMemberApiClient.GetMembersClientInstanceIds(sessionId, cancellationToken);
            
            memberIdsToCheck = new List<string>(sessionMemberInstanceIds);
        }
        
        if (memberIdsToCheck.Count == 0)
        {
            LogProblem("Members list is empty");
            
            return JoinSessionResult.BuildFrom(JoinSessionStatus.SessionNotFound);
        }
        
        memberIdsToCheck.RemoveAll(id => id.StartsWith(_environmentService.ClientId));
        if (memberIdsToCheck.Count == 0)
        {
            return JoinSessionResult.BuildProcessingNormally();
        }
        
        var parameters = new TrustCheckParameters
        {
            SessionId = sessionId,
            PublicKeyInfo = _publicKeysManager.GetMyPublicKeyInfo(),
            MembersInstanceIdsToCheck = memberIdsToCheck,
            ProtocolVersion = ProtocolVersion.CURRENT
        };
        
        await _trustProcessPublicKeysRepository.ResetJoinerTrustProcessData(sessionId);
        var result = await _trustApiClient.StartTrustCheck(parameters, cancellationToken);
        
        if (result == null || !result.IsOK)
        {
            _logger.LogError("Can not start trust check");
            
            return JoinSessionResult.BuildFrom(JoinSessionStatus.UnexpectedError);
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
            
            return JoinSessionResult.BuildFrom(JoinSessionStatus.TrustCheckFailed);
        }
    }
    
    private void LogProblem(string problemDescription, [CallerMemberName] string caller = "")
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        _logger.LogWarning($"CloudSessionConnector.{caller}: {problemDescription}");
    }
}