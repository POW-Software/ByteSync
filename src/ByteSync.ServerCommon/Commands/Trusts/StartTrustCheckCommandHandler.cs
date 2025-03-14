using ByteSync.Common.Business.Sessions.Cloud.Connections;
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

        _logger.LogInformation("StartTrustCheck: {Joiner} starts trust check for session {SessionId}. {Count} members to check", 
            joiner.ClientInstanceId, trustCheckParameters.SessionId, trustCheckParameters.MembersInstanceIdsToCheck.Count);
        
        List<string> members = new List<string>();
        foreach (var clientInstanceId in trustCheckParameters.MembersInstanceIdsToCheck)
        {
            if (cloudSession.SessionMembers.Any(sm => sm.ClientInstanceId == clientInstanceId))
            {
                members.Add(clientInstanceId);
                
                _logger.LogInformation("StartTrustCheck: {Member} must be trusted by {Joiner}", 
                    clientInstanceId, joiner.ClientInstanceId);
                
                await _invokeClientsService.Client(clientInstanceId).AskPublicKeyCheckData(trustCheckParameters.SessionId, joiner.ClientInstanceId,
                    trustCheckParameters.PublicKeyInfo).ConfigureAwait(false);
            }
        }
        
        return new StartTrustCheckResult { IsOK = true, MembersInstanceIds = members };
    }
}