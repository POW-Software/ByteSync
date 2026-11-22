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
            ByteSync.ServerCommon.Security.SessionAuthorizationBehavior<object, object>.RequireIdentity(request.Client);
            var recipient = session.FindMemberOrPreMember(request.Parameters.OtherPartyClientInstanceId);
            if (session.IsSessionRemoved || session.IsSessionActivated ||
                session.AuthenticationEpoch != ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch ||
                session.ProtocolVersion != ByteSync.Common.Business.Versions.ProtocolVersion.CURRENT ||
                session.FindMemberOrPreMember(request.Client.ClientInstanceId)?.Identity != request.Client.Identity ||
                recipient?.Identity == null ||
                (session.FindMember(request.Client.ClientInstanceId) == null &&
                    session.FindMemberOrPreMember(request.Client.ClientInstanceId)!.JoinExpiresAt <= DateTimeOffset.UtcNow) ||
                (session.FindMember(recipient.ClientInstanceId) == null && recipient.JoinExpiresAt <= DateTimeOffset.UtcNow) ||
                !MatchesIdentity(request.Parameters.OtherPartyPublicKey, recipient.Identity) ||
                string.IsNullOrWhiteSpace(request.Parameters.ExchangeId) || request.Parameters.ExchangeId.Length > 128)
                throw new UnauthorizedAccessException("Invalid peer trust confirmation");
            request.Parameters.IssuerClientId = request.Client.ClientId;
            request.Parameters.IssuerClientInstanceId = request.Client.ClientInstanceId;
            request.Parameters.IssuerIdentity = request.Client.Identity;
            await _invokeClientsService.Client(request.Parameters.OtherPartyClientInstanceId).InformPublicKeyValidationIsFinished(request.Parameters).ConfigureAwait(false);
            
            _logger.LogInformation("InformPublicKeyValidationIsFinished: {Sender} sends PublicKeyValidation to {Recipient}", request.Client.ClientInstanceId,
                request.Parameters.OtherPartyClientInstanceId);
        }
        else
        { 
            _logger.LogInformation("InformPublicKeyValidationIsFinished: session not found for sessionId '{sessionId}'. Can not proceed",
                request.Parameters.SessionId);
        }
    }
    
    private static bool MatchesIdentity(byte[]? key, string identity)
    {
        try
        {
            return key is { Length: > 0 and <= 2048 } &&
                ByteSync.Common.Business.Auth.AuthenticationProtocol.GetIdentity(key) == identity;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }
}