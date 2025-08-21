using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class RequestTrustPublicKeyCommandHandler: IRequestHandler<RequestTrustPublicKeyRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    public readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<SetAuthCheckedCommandHandler> _logger;
    
    public RequestTrustPublicKeyCommandHandler(ICloudSessionsRepository cloudSessionsRepository, IInvokeClientsService invokeClientService, ILogger<SetAuthCheckedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientService;
        _logger = logger;
    }

    public async Task Handle(RequestTrustPublicKeyRequest request, CancellationToken cancellationToken)
    {
        var recipient = await _cloudSessionsRepository.GetSessionMember(request.Parameters.SessionId, request.Parameters.SessionMemberInstanceId);
        if (recipient != null)
        {
            await _invokeClientsService.Client(recipient).RequestTrustPublicKey(request.Parameters).ConfigureAwait(false);
            
            _logger.LogInformation("RequestTrustPublicKey: {Sender} sends trust publicKey Request to {Recipient}", request.Client.ClientInstanceId,
                request.Parameters.SessionMemberInstanceId);
        }
        else
        { 
            _logger.LogInformation("InformPublicKeyValidationIsFinished: Recipient not found'. Can not proceed");
        }
    }
    
}