using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class StartTrustCheckCommandHandler : IRequestHandler<StartTrustCheckRequest, StartTrustCheckResult>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<StartTrustCheckCommandHandler> _logger;

    public StartTrustCheckCommandHandler(ICloudSessionsRepository cloudSessionsRepository, IInvokeClientsService invokeClientsService, 
        ILogger<StartTrustCheckCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }
    
    public async Task<StartTrustCheckResult> Handle(StartTrustCheckRequest request, CancellationToken cancellationToken)
    {
        var trustCheckParameters = request.Parameters;
        var joiner = request.Client;
        
        var cloudSession = await _cloudSessionsRepository.Get(trustCheckParameters.SessionId).ConfigureAwait(false);
        if (cloudSession == null)
        {
            return new StartTrustCheckResult { IsOK = false };
        }

        var joinerProtocolVersion = trustCheckParameters.ProtocolVersion;
        
        if (!ProtocolVersion.IsCompatible(joinerProtocolVersion))
        {
            _logger.LogWarning(
                "StartTrustCheck: Joiner {JoinerId} has incompatible protocol version {JoinerVersion}",
                joiner.ClientInstanceId, joinerProtocolVersion);
            
            return new StartTrustCheckResult { IsOK = false, IsProtocolVersionIncompatible = true };
        }
        
        var membersToCheck = cloudSession.SessionMembers
            .Where(m => trustCheckParameters.MembersInstanceIdsToCheck.Contains(m.ClientInstanceId));
        
        foreach (var member in membersToCheck)
        {
            var memberProtocolVersion = member.PublicKeyInfo.ProtocolVersion;
            
            if (!ProtocolVersion.IsCompatible(memberProtocolVersion) || 
                memberProtocolVersion != joinerProtocolVersion)
            {
                _logger.LogWarning(
                    "StartTrustCheck: Protocol version mismatch between joiner {JoinerId} (version {JoinerVersion}) and member {MemberId} (version {MemberVersion})",
                    joiner.ClientInstanceId, joinerProtocolVersion, member.ClientInstanceId, memberProtocolVersion);
                
                return new StartTrustCheckResult { IsOK = false, IsProtocolVersionIncompatible = true };
            }
        }

        _logger.LogInformation("StartTrustCheck: {Joiner} starts trust check for session {SessionId}. {Count} members to check", 
            joiner.ClientInstanceId, trustCheckParameters.SessionId, trustCheckParameters.MembersInstanceIdsToCheck.Count);
        
        var validMemberIds = trustCheckParameters.MembersInstanceIdsToCheck
            .Where(id => cloudSession.SessionMembers.Any(sm => sm.ClientInstanceId == id))
            .ToList();
        
        foreach (var clientInstanceId in validMemberIds)
        {
            _logger.LogInformation("StartTrustCheck: {Member} must be trusted by {Joiner}", 
                clientInstanceId, joiner.ClientInstanceId);
            
            await _invokeClientsService.Client(clientInstanceId).AskPublicKeyCheckData(trustCheckParameters.SessionId, joiner.ClientInstanceId,
                trustCheckParameters.PublicKeyInfo).ConfigureAwait(false);
        }
        
        return new StartTrustCheckResult { IsOK = true, MembersInstanceIds = validMemberIds };
    }
}