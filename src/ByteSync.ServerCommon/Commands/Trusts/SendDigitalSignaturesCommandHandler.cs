using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class SendDigitalSignaturesCommandHandler : IRequestHandler<SendDigitalSignaturesRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ILobbyRepository _lobbyRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<SendDigitalSignaturesCommandHandler> _logger;

    public SendDigitalSignaturesCommandHandler(
        ICloudSessionsRepository cloudSessionsRepository,
        ILobbyRepository lobbyRepository,
        IInvokeClientsService invokeClientsService,
        ILogger<SendDigitalSignaturesCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _lobbyRepository = lobbyRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }
    
    public async Task Handle(SendDigitalSignaturesRequest request, CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var client = request.Client;
        
        if (parameters.DigitalSignatureCheckInfos.Any(ds => !ds.Issuer.Equals(client.ClientInstanceId)))
        {
            _logger.LogInformation("{Endpoint} must always be the issuer of the Digital Signature", client.ClientInstanceId);
            return;
        }
        
        var cloudSession = await _cloudSessionsRepository.Get(parameters.DataId).ConfigureAwait(false);
        Lobby? lobby = null;
        if (cloudSession != null)
        {
            if (cloudSession.FindMemberOrPreMember(client.ClientInstanceId) == null)
            {
                _logger.LogInformation("{Endpoint} is neither a member nor a premember of session {session}", client.ClientInstanceId, parameters.DataId);
                return;
            }
        }
        else
        {
            lobby = await _lobbyRepository.Get(parameters.DataId).ConfigureAwait(false);
            if (lobby != null)
            {
                if (lobby.GetLobbyMemberByClientInstanceId(client.ClientInstanceId) == null)
                {
                    _logger.LogInformation("{Endpoint} is neither a member of lobby {lobbyId}", client.ClientInstanceId, parameters.DataId);
                    return;
                }
            }
        }

        if (cloudSession != null || lobby != null)
        {
            if (cloudSession != null && parameters.IsAuthCheckOK)
            {
                await _cloudSessionsRepository.Update(cloudSession.SessionId, cloudSessionData =>
                {
                    var member = cloudSessionData.FindMemberOrPreMember(client.ClientInstanceId);

                    if (member != null)
                    {
                        foreach (var digitalSignatureCheckInfo in parameters.DigitalSignatureCheckInfos)
                        {
                            member.AuthCheckClientInstanceIds.Add(digitalSignatureCheckInfo.Recipient);
                        }

                        return true;
                    }

                    return false;
                });
            }
            
            foreach (var digitalSignatureCheckInfo in parameters.DigitalSignatureCheckInfos)
            {
                await _invokeClientsService.Client(digitalSignatureCheckInfo.Recipient).RequestCheckDigitalSignature(digitalSignatureCheckInfo).ConfigureAwait(false);
            }
        }
        else
        {
            _logger.LogInformation("SendDigitalSignatures: session or lobby not found for Id '{dataId}'. Can not proceed", parameters.DataId);
        }
    }
}