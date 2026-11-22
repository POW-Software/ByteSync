using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class FinalizeJoinCloudSessionCommandHandler : IRequestHandler<FinalizeJoinCloudSessionRequest, FinalizeJoinSessionResult>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISessionMemberMapper _sessionMemberMapper;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly IClientsGroupsService _clientsGroupsService;
    private readonly IRedisInfrastructureService _redisInfrastructureService;
    private readonly ILogger<FinalizeJoinCloudSessionCommandHandler> _logger;
    
    public FinalizeJoinCloudSessionCommandHandler(ICloudSessionsRepository cloudSessionsRepository, ISessionMemberMapper sessionMemberMapper,
        IInvokeClientsService invokeClientsService, IClientsGroupsService clientsGroupsService, 
        IRedisInfrastructureService redisInfrastructureService, ILogger<FinalizeJoinCloudSessionCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientsService;
        _clientsGroupsService = clientsGroupsService;
        _sessionMemberMapper = sessionMemberMapper;
        _redisInfrastructureService = redisInfrastructureService;
        _logger = logger;
    }
    
    public async Task<FinalizeJoinSessionResult> Handle(FinalizeJoinCloudSessionRequest request, CancellationToken cancellationToken)
    {
        var parameters = request.FinalizeJoinCloudSessionParameters;
        var client = request.Client;
        if (parameters.JoinerInstanceId != client.ClientInstanceId) throw new UnauthorizedAccessException();
        
        FinalizeJoinSessionStatuses? finalizeJoinSessionStatus = null;
        SessionMemberData? joiner = null;
        
        await using var sessionLock = await _redisInfrastructureService.AcquireLockAsync(ByteSync.ServerCommon.Entities.EntityType.Session, parameters.SessionId);
        var transaction = _redisInfrastructureService.OpenTransaction();
        
        var updateResult = await _cloudSessionsRepository.Update(parameters.SessionId, innerCloudSessionData =>
        {
            if (innerCloudSessionData.IsSessionRemoved)
            {
                finalizeJoinSessionStatus = FinalizeJoinSessionStatuses.SessionNotFound;
            }
            else if (innerCloudSessionData.IsSessionActivated)
            {
                finalizeJoinSessionStatus = FinalizeJoinSessionStatuses.SessionAlreadyActivated;
            }
            else if (innerCloudSessionData.SessionMembers
                     .Count(sm => !sm.IsAuthCheckedFor(parameters.JoinerInstanceId, client.Identity)) > 0)
            {
                var nonAuthCheckedMembers = innerCloudSessionData.SessionMembers
                    .Where(sm => !sm.IsAuthCheckedFor(parameters.JoinerInstanceId, client.Identity))
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
                        Equals(m.FinalizationPassword, parameters.FinalizationPassword) &&
                        !string.IsNullOrEmpty(m.FinalizationPassword) && m.Identity == client.Identity &&
                        m.JoinExpiresAt > DateTimeOffset.UtcNow &&
                        innerCloudSessionData.FindMember(parameters.ValidatorInstanceId) != null);

                if (joiner == null)
                {
                    finalizeJoinSessionStatus = FinalizeJoinSessionStatuses.PrememberNotFound;
                }
            }

            if (joiner != null && finalizeJoinSessionStatus == null)
            {
                joiner.FinalizationPassword = "";
                joiner.EncryptedPrivateData = parameters.EncryptedSessionMemberPrivateData;
                    
                innerCloudSessionData.SessionMembers.Remove(joiner);
                innerCloudSessionData.SessionMembers.Add(joiner);
                innerCloudSessionData.PreSessionMembers.Remove(joiner);
                
                finalizeJoinSessionStatus = FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess;

                return true;
            }
            else
            {
                return false;
            }
        }, transaction, sessionLock);
        
        if (updateResult.IsWaitingForTransaction)
        {
            var sessionMemberInfo = await _sessionMemberMapper.Convert(joiner!);

            await _clientsGroupsService.AddSessionSubscription(client, parameters.SessionId, transaction);

            if (!await transaction.ExecuteAsync()) throw new InvalidOperationException("Join transaction was not committed");
            
            await _invokeClientsService.SessionGroup(parameters.SessionId).MemberJoinedSession(sessionMemberInfo).ConfigureAwait(false);
            await _clientsGroupsService.AddToSessionGroup(client, parameters.SessionId).ConfigureAwait(false);
            
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
}