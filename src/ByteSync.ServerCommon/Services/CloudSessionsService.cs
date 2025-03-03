using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class CloudSessionsService : ICloudSessionsService
{
    private readonly ILogger<CloudSessionsService> _logger;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly IByteSyncClientCaller _byteSyncClientCaller;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISynchronizationService _synchronizationService;
    private readonly IInventoryService _inventoryService;
    private readonly ISessionMemberMapper _sessionMemberConverter;

    public CloudSessionsService(ILogger<CloudSessionsService> logger, ISharedFilesService sharedFilesService, IByteSyncClientCaller byteSyncClientCaller, 
        ICloudSessionsRepository cloudSessionsRepository, ISynchronizationService synchronizationService, IInventoryService inventoryService,
        ISessionMemberMapper sessionMemberConverter)
    {
        _logger = logger;
        _sharedFilesService = sharedFilesService;
        _byteSyncClientCaller = byteSyncClientCaller;
        _cloudSessionsRepository = cloudSessionsRepository;
        _synchronizationService = synchronizationService;
        _inventoryService = inventoryService;
        _sessionMemberConverter = sessionMemberConverter;
    }
    
    public async Task<CloudSessionResult> CreateCloudSession(CreateCloudSessionParameters createCloudSessionParameters, Client client)
    {
        CloudSessionData cloudSessionData;
        SessionMemberData creatorData;
        
        cloudSessionData = new CloudSessionData(createCloudSessionParameters.LobbyId, createCloudSessionParameters.SessionSettings, client);
        creatorData = new SessionMemberData(client, createCloudSessionParameters.CreatorPublicKeyInfo, 
            createCloudSessionParameters.CreatorProfileClientId, cloudSessionData, 
            createCloudSessionParameters.CreatorPrivateData);
        cloudSessionData.SessionMembers.Add(creatorData);

        cloudSessionData = await _cloudSessionsRepository.AddCloudSession(cloudSessionData, GenerateRandomSessionId);

        await _byteSyncClientCaller.AddToSessionGroup(client, cloudSessionData.SessionId);

        _logger.LogInformation("Cloud Session {SessionId} created", cloudSessionData.SessionId);

        var cloudSessionResult = await BuildCloudSessionResult(cloudSessionData, creatorData);

        return cloudSessionResult;
    }

    public async Task<List<string>> GetMembersInstanceIds(string sessionId)
    {
        var cloudSession = await _cloudSessionsRepository.Get(sessionId);

        List<string> result = new List<string>();
            
        if (cloudSession != null)
        {
            var allMembers = cloudSession.SessionMembers;
            
            result.AddAll(allMembers.Select(m => m.ClientInstanceId));
        }
        else
        {
            _logger.LogInformation("GetMembersInstanceIds: session not found for sessionId '{sessionId}'. Can not proceed", sessionId);
        }

        return result;
    }

    private string GenerateRandomSessionId()
    {
        string sessionId = RandomUtils.GetRandomNumber(3) + 
                    RandomUtils.GetRandomLetters(3, false) +
                    RandomUtils.GetRandomNumber(3);

        return sessionId;
    }

    public async Task PreJoinCloudSession(Client client, PublicKeyInfo publicKeyInfo,
        string? profileClientId,
        string sessionId, string validatorClientInstanceId)
    {
        //var cloudSessionData = await _cloudSessionsRepository.GetCloudSession(sessionId);
        
        var updateResult = await _cloudSessionsRepository.Update(sessionId, cloudSessionData =>
        {
            SessionMemberData joiner = new SessionMemberData(client, publicKeyInfo, profileClientId, cloudSessionData);
            joiner.ValidatorInstanceId = validatorClientInstanceId;
                        
            // On enlève tout ce qui peut ressemble au joiner de la liste
            cloudSessionData.PreSessionMembers.Remove(joiner);
            cloudSessionData.PreSessionMembers.RemoveAll(m => m.ClientInstanceId.Equals(joiner.ClientInstanceId));
                        
            cloudSessionData.PreSessionMembers.Add(joiner);

            return true;
        });
    }

    public async Task<JoinSessionResult> AskCloudSessionPasswordExchangeKey(Client client,
        AskCloudSessionPasswordExchangeKeyParameters parameters)
    {
        var cloudSession = await _cloudSessionsRepository.Get(parameters.SessionId);

        if (cloudSession == null)
        {
            _logger.LogInformation("AskCloudSessionPasswordExchangeKey: session not found for sessionId '{sessionId}'. Can not proceed",
                parameters.SessionId);

            return JoinSessionResult.BuildFrom(JoinSessionStatus.SessionNotFound);
        }
        
        JoinSessionResult? result = await PrecheckJoinSession(client, cloudSession);
        if (result != null)
        {
            return result;
        }
        
        var member = cloudSession.SessionMembers.FirstOrDefault();
        if (member != null)
        {
            await PreJoinCloudSession(client, parameters.PublicKeyInfo, 
                parameters.ProfileClientId, parameters.SessionId, member.ClientInstanceId).ConfigureAwait(false);
            
            _logger.LogInformation("AskCloudSessionPasswordExchangeKey: Asking PasswordExchangeKey to {member} for session {sessionId} " +
                                "and requester {requester}", member.BuildLog(), parameters.SessionId, client.ClientInstanceId);

            var pushData = new AskCloudSessionPasswordExchangeKeyPush
            {
                SessionId = parameters.SessionId,
                PublicKeyInfo = parameters.PublicKeyInfo,
                RequesterInstanceId = client.ClientInstanceId,
            };
            await _byteSyncClientCaller.Client(member.ClientInstanceId).AskCloudSessionPasswordExchangeKey(pushData).ConfigureAwait(false);

            return JoinSessionResult.BuildProcessingNormally();
        }
        else
        {
            _logger.LogInformation("AskCloudSessionPasswordExchangeKey: no member found for sessionId '{sessionId}'. Can not proceed",
                parameters.SessionId);

            return JoinSessionResult.BuildFrom(JoinSessionStatus.TransientError);
        }
    }
    
    private async Task<JoinSessionResult?> PrecheckJoinSession(Client client, CloudSessionData cloudSession)
    {
        var allMembers = cloudSession.SessionMembers;
        
        bool isLimitOK = await CheckCanJoinLimit(client, allMembers).ConfigureAwait(false);
        if (!isLimitOK)
        {
            _logger.LogInformation("AskCloudSessionPasswordExchangeKey: already too many members ({members}) for sessionId '{sessionId}'. Can not proceed",
                allMembers.Count, cloudSession.SessionId);

            return JoinSessionResult.BuildFrom(JoinSessionStatus.TooManyMembers);
        }
            
        if (cloudSession.IsSessionActivated)
        {
            _logger.LogInformation("AskCloudSessionPasswordExchangeKey: session '{sessionId}' is already activated. Can not proceed",
                cloudSession.SessionId);

            return JoinSessionResult.BuildFrom(JoinSessionStatus.SessionAlreadyActivated);
        }

        return null;
    }
    
    private async Task<bool> CheckCanJoinLimit(Client currentClient, List<SessionMemberData> allMembers)
    {
        HashSet<Client> clients = new HashSet<Client> { currentClient };

        int limit = 5;

        bool result = allMembers.Count < limit;

        return result;
    }
    
    public async Task ValidateJoinCloudSession(ValidateJoinCloudSessionParameters parameters)
    {
        SessionMemberData? joiner = null;
        
        var updateResult = await _cloudSessionsRepository.Update(parameters.SessionId, cloudSessionData =>
        {
            if (cloudSessionData is { IsSessionActivated: false, IsSessionRemoved: false })
            {
                joiner = cloudSessionData
                    .PreSessionMembers
                    .FirstOrDefault(m =>
                        Equals(m.ClientInstanceId, parameters.JoinerClientInstanceId) && 
                        Equals(m.ValidatorInstanceId, parameters.ValidatorInstanceId));

                if (joiner != null)
                {
                    var finaliationPassword = $"FP_{Guid.NewGuid()}";

                    joiner.FinalizationPassword = finaliationPassword;
                    parameters.FinalizationPassword = finaliationPassword;
                }

                return true;
            }
            else
            {
                return false;
            }
        });
        
        if (updateResult.IsSaved)
        {
            _logger.LogInformation("ValidateJoinCloudSession: {@cloudSession} by {@joiner}", updateResult.Element.BuildLog(), joiner!.BuildLog());

            var cloudSessionResult = await BuildCloudSessionResult(updateResult.Element, joiner!);

            await _byteSyncClientCaller.Client(joiner!).YouJoinedSession(cloudSessionResult, parameters);
        }
    }

    public async Task<FinalizeJoinSessionResult> FinalizeJoinCloudSession(Client client, FinalizeJoinCloudSessionParameters parameters)
    {
        FinalizeJoinSessionStatuses? finalizeJoinSessionStatus = null;
        SessionMemberData? joiner = null;

        var updateResult = await _cloudSessionsRepository.Update(parameters.SessionId, innerCloudSessionData =>
        {
            if (innerCloudSessionData.IsSessionRemoved || innerCloudSessionData.IsSessionActivated)
            {
                finalizeJoinSessionStatus = FinalizeJoinSessionStatuses.SessionNotFound;
            }
            else if (innerCloudSessionData.IsSessionActivated)
            {
                finalizeJoinSessionStatus = FinalizeJoinSessionStatuses.SessionAlreadyActivated;
            }
            else if (innerCloudSessionData.SessionMembers
                     .Count(sm => !sm.IsAuthCheckedFor(parameters.JoinerInstanceId)) > 0)
            {
                var nonAuthCheckedMembers = innerCloudSessionData.SessionMembers
                    .Where(sm => !sm.IsAuthCheckedFor(parameters.JoinerInstanceId))
                    .Select(sm => sm.ClientInstanceId)
                    .ToList().JoinToString(",");

                _logger.LogInformation("FinalizeJoinCloudSession: session {SessionId} has non-auth checked members {NonAuthCheckedMembers}",
                    parameters.SessionId, nonAuthCheckedMembers);
                
                finalizeJoinSessionStatus = FinalizeJoinSessionStatuses.AuthIsNotChecked;
            }
            else
            {
               joiner = innerCloudSessionData
                    .PreSessionMembers
                    .FirstOrDefault(m =>
                        Equals(m.ClientInstanceId, parameters.JoinerInstanceId) && 
                        Equals(m.ValidatorInstanceId, parameters.ValidatorInstanceId) &&
                        Equals(m.FinalizationPassword, parameters.FinalizationPassword));

                if (joiner == null)
                {
                    finalizeJoinSessionStatus = FinalizeJoinSessionStatuses.PrememberNotFound;
                }
            }

            if (joiner != null && finalizeJoinSessionStatus == null)
            {
                if (!innerCloudSessionData!.SessionMembers.Any(smd => smd.ClientInstanceId.Equals(joiner.ClientInstanceId)))
                {
                    joiner.EncryptedPrivateData = parameters.EncryptedSessionMemberPrivateData;
                    
                    innerCloudSessionData.SessionMembers.Add(joiner);
                    innerCloudSessionData.PreSessionMembers.Remove(joiner);
                }
                
                finalizeJoinSessionStatus = FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess;

                return true;
            }
            else
            {
                return false;
            }
        });
        
        if (updateResult.IsSaved)
        {
            var sessionMemberInfo = await _sessionMemberConverter.Convert(joiner!);
            
            await _byteSyncClientCaller.SessionGroup(parameters.SessionId).MemberJoinedSession(sessionMemberInfo).ConfigureAwait(false);
            await _byteSyncClientCaller.AddToSessionGroup(client, parameters.SessionId).ConfigureAwait(false);
            
            _logger.LogInformation("FinalizeJoinCloudSession: {@cloudSession} by {@joiner}", 
                joiner!.CloudSessionData.BuildLog(), joiner.BuildLog());
        }
        else
        {
            _logger.LogInformation("FinalizeJoinCloudSession: Can not validate member {JoinerId} for session {SessionId}, status: {Status}", 
                parameters.JoinerInstanceId, parameters.SessionId, finalizeJoinSessionStatus);
        }
            
        FinalizeJoinSessionResult finalizeJoinSessionResult = FinalizeJoinSessionResult.BuildFrom(finalizeJoinSessionStatus!.Value);
            
        return finalizeJoinSessionResult;
    }

    private async Task<CloudSessionResult> BuildCloudSessionResult(CloudSessionData cloudSessionData, SessionMemberData sessionMemberData)
    {
        var sessionMemberInfo = await _sessionMemberConverter.Convert(sessionMemberData);

        CloudSessionResult cloudSessionResult = new CloudSessionResult(cloudSessionData.GetCloudSession(), cloudSessionData.SessionSettings, sessionMemberInfo);

        foreach (var sessionMember in cloudSessionData.SessionMembers)
        {
            cloudSessionResult.MembersIds.Add(sessionMember.ClientInstanceId);
        }

        return cloudSessionResult;
    }

    public async Task<List<SessionMemberInfoDTO>> GetSessionMembersInfosAsync(string sessionId)
    {
        var cloudSession = await _cloudSessionsRepository.Get(sessionId);

        if (cloudSession == null)
        {
            return new List<SessionMemberInfoDTO>();
        }

        List<SessionMemberInfoDTO> result = new List<SessionMemberInfoDTO>();
        foreach (var sessionMemberData in cloudSession.SessionMembers)
        {
            var sessionMemberInfo = await _sessionMemberConverter.Convert(sessionMemberData);

            result.Add(sessionMemberInfo);
        }

        return result;
    }

    // public async Task UpdateSessionSettings(Client client, string sessionId, EncryptedSessionSettings sessionSettings)
    // {
    //     // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    //     if (sessionSettings == null)
    //     {
    //         _logger.LogWarning("UpdateSessionSettings: sessionSettings null");
    //         return;
    //     }
    //
    //     var result = await _cloudSessionsRepository.Update(sessionId, cloudSessionData =>
    //     {
    //         cloudSessionData.UpdateSessionSettings(sessionSettings);
    //         
    //         _logger.LogInformation("UpdateSessionSettings: {@cloudSession}", cloudSessionData.BuildLog());
    //
    //         return true;
    //     });
    //
    //     if (result.IsSaved)
    //     {
    //         var sessionSettingsUpdatedDto = new SessionSettingsUpdatedDTO(sessionId, client.ClientInstanceId, sessionSettings);
    //         
    //         await _byteSyncClientCaller.SessionGroupExcept(sessionId, client).SessionSettingsUpdated(sessionSettingsUpdatedDto).ConfigureAwait(false);
    //     }
    // }
    
    public async Task<bool> ResetSession(string sessionId, Client client)
    {
        await _cloudSessionsRepository.Update(sessionId, cloudSessionData =>
        {
            cloudSessionData.ResetSession();
            
            return true;
        });
        
        await _inventoryService.ResetSession(sessionId);

        await _synchronizationService.ResetSession(sessionId);
        
        await _sharedFilesService.ClearSession(sessionId);
        
        _logger.LogInformation("ResetSession: session {sessionId} reset by {clientInstanceId}", sessionId, client.ClientInstanceId);
        
        await _byteSyncClientCaller.SessionGroupExcept(sessionId, client)
            .SessionResetted(new BaseSessionDto(sessionId, client.ClientInstanceId));

        return true;
    }
    
    public async Task<JoinSessionResult> AskJoinCloudSession(Client client, AskJoinCloudSessionParameters parameters)
    {
        if (!client.ClientInstanceId.Equals(parameters.JoinerClientInstanceId))
        {
            _logger.LogWarning("AskJoinCloudSession: ClientInstanceIds not matching. client.ClientInstanceId:{id1}" +
                        "parameters.JoinerId:{id2}", client.ClientInstanceId, parameters.JoinerClientInstanceId);
            return JoinSessionResult.BuildFrom(JoinSessionStatus.TransientError);
        }

        var cloudSessionData = await _cloudSessionsRepository.Get(parameters.SessionId);

        if (cloudSessionData != null)
        {
            var members = cloudSessionData.SessionMembers;

            bool isLimitOK = await CheckCanJoinLimit(client, members).ConfigureAwait(false);
            if (!isLimitOK)
            {
                _logger.LogInformation(
                    "AskJoinCloudSession: already too many members ({members}) for sessionId '{sessionId}'. Can not proceed",
                    members.Count, parameters.SessionId);
                return JoinSessionResult.BuildFrom(JoinSessionStatus.TooManyMembers);
            }

            if (cloudSessionData.IsSessionActivated)
            {
                _logger.LogInformation(
                    "AskJoinCloudSession: session '{sessionId}' is already activated. Can not proceed",
                    parameters.SessionId);
                return JoinSessionResult.BuildFrom(JoinSessionStatus.SessionAlreadyActivated);
            }

            var member = members.FirstOrDefault(m => m.ClientInstanceId.Equals(parameters.ValidatorInstanceId));

            if (member != null)
            {
                await _byteSyncClientCaller.Client(member).CheckCloudSessionPasswordExchangeKey(parameters).ConfigureAwait(false);

                return JoinSessionResult.BuildProcessingNormally();
            }
            else
            {
                _logger.LogInformation(
                    "AskJoinCloudSession: not member found for sessionId '{sessionId}' and clientInstanceId:{cid}. Can not proceed",
                    parameters.SessionId, parameters.ValidatorInstanceId);

                return JoinSessionResult.BuildFrom(JoinSessionStatus.TransientError);
            }
        }
        else
        {
            _logger.LogInformation(
                "AskJoinCloudSession: session not found for sessionId '{sessionId}'. Can not proceed",
                parameters.SessionId);

            return JoinSessionResult.BuildFrom(JoinSessionStatus.SessionNotFound);
        }
    }
    
    public async Task InformPasswordIsWrong(Client client, string sessionId, string clientInstanceId)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionPreMember(sessionId, clientInstanceId);

        if (sessionMemberData != null && sessionMemberData.ValidatorInstanceId.IsNotEmpty()
                                      && Equals(sessionMemberData.ValidatorInstanceId, client.ClientInstanceId))
        {
            await _byteSyncClientCaller.Client(sessionMemberData).YouGaveAWrongPassword(sessionId).ConfigureAwait(false);
        }
    }
    
    public async Task GiveCloudSessionPasswordExchangeKey(Client client, GiveCloudSessionPasswordExchangeKeyParameters parameters)
    {
        var preSessionMemberData = await _cloudSessionsRepository.GetSessionPreMember(parameters.SessionId, parameters.JoinerInstanceId);
        if (preSessionMemberData == null)
        {
            _logger.LogWarning("GiveCloudSessionPasswordExchangeKey: PreSessionMember not found for {session}", parameters.SessionId);
            return;
        }

        if (!Equals(preSessionMemberData.ValidatorInstanceId, client.ClientInstanceId))
        {
            _logger.LogWarning("GiveCloudSessionPasswordExchangeKey: Unexpected ValidatorClientInstanceId for {session}", parameters.SessionId);
            return;
        }

        _logger.LogInformation("GiveCloudSessionPasswordExchangeKey: Giving PasswordExchangeKey to {clientDestination}",
            preSessionMemberData.BuildLog());
        await _byteSyncClientCaller.Client(preSessionMemberData).GiveCloudSessionPasswordExchangeKey(parameters).ConfigureAwait(false);
    }
}