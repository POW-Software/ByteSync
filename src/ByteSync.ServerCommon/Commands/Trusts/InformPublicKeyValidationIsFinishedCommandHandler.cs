using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class InformPublicKeyValidationIsFinishedCommandHandler: IRequestHandler<InformPublicKeyValidationIsFinishedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<InformPublicKeyValidationIsFinishedCommandHandler> _logger;

    public InformPublicKeyValidationIsFinishedCommandHandler(ICloudSessionsRepository cloudSessionsRepository, IInvokeClientsService invokeClientService, ILogger<InformPublicKeyValidationIsFinishedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientService;
        _logger = logger;
    }
   
    public async Task Handle(InformPublicKeyValidationIsFinishedRequest request, CancellationToken cancellationToken)
    {
        var session = await _cloudSessionsRepository.Get(request.Parameters.SessionId);
        if (session != null)
        {
            await _invokeClientsService.Client(request.Parameters.OtherPartyClientInstanceId).InformPublicKeyValidationIsFinished(request.Parameters).ConfigureAwait(false);
            
            _logger.LogInformation("InformPublicKeyValidationIsFinished: {Sender} sends PublicKeyValidation to {Recipient}", request.Client.ClientInstanceId,
                request.Parameters.OtherPartyClientInstanceId);
        }
        else
        { 
            _logger.LogInformation("AskCloudSessionMembersPublicKeys: session not found for sessionId '{sessionId}'. Can not proceed",
                request.Parameters.SessionId);
        }
    }
    
}