using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class GiveMemberPublicKeyCheckDataCommandHandler : IRequestHandler<GiveMemberPublicKeyCheckDataRequest>
{
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<GiveMemberPublicKeyCheckDataCommandHandler> _logger;

    public GiveMemberPublicKeyCheckDataCommandHandler(IInvokeClientsService invokeClientsService, ILogger<GiveMemberPublicKeyCheckDataCommandHandler> logger)
    {
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }
    
    public async Task Handle(GiveMemberPublicKeyCheckDataRequest request, CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var client = request.Client;
        
        await _invokeClientsService.Client(parameters.ClientInstanceId).GiveMemberPublicKeyCheckData(parameters.SessionId, parameters.PublicKeyCheckData).ConfigureAwait(false);
            
        _logger.LogInformation("GiveMemberPublicKeyCheckData: {Sender} gives PublicKeyCheckData to {Recipient}", client.ClientInstanceId,
            parameters.ClientInstanceId);
    }
}