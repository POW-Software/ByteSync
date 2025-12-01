using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class InformProtocolVersionIncompatibleCommandHandler : IRequestHandler<InformProtocolVersionIncompatibleRequest>
{
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<InformProtocolVersionIncompatibleCommandHandler> _logger;

    public InformProtocolVersionIncompatibleCommandHandler(IInvokeClientsService invokeClientsService, ILogger<InformProtocolVersionIncompatibleCommandHandler> logger)
    {
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }
    
    public async Task Handle(InformProtocolVersionIncompatibleRequest request, CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var client = request.Client;
        
        await _invokeClientsService.Client(parameters.JoinerClientInstanceId)
            .InformProtocolVersionIncompatible(parameters)
            .ConfigureAwait(false);
            
        _logger.LogWarning(
            "InformProtocolVersionIncompatible: Member {MemberClientInstanceId} (version {MemberVersion}) informed Joiner {JoinerClientInstanceId} (version {JoinerVersion}) of protocol version incompatibility",
            parameters.MemberClientInstanceId, 
            parameters.MemberProtocolVersion,
            parameters.JoinerClientInstanceId,
            parameters.JoinerProtocolVersion);
    }
}

