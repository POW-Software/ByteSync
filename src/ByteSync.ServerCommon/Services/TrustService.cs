using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class TrustService : ITrustService
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<TrustService> _logger;


    public TrustService(ICloudSessionsRepository cloudSessionsRepository,
        IInvokeClientsService invokeClientsService, ILogger<TrustService> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }

    public async Task SetAuthChecked(Client client, SetAuthCheckedParameters parameters)
    {
        await _cloudSessionsRepository.Update(parameters.SessionId, cloudSessionData =>
        {
            var member = cloudSessionData.FindMemberOrPreMember(client.ClientInstanceId);
            
            if (member == null)
            {
                _logger.LogInformation("{Endpoint} is neither a member nor a premember of session {session}", 
                    client.ClientInstanceId, parameters.SessionId);
                return false;
            }
            
            member.AuthCheckClientInstanceIds.Add(parameters.CheckedClientInstanceId);

            return true;
        });
    }

    public async Task RequestTrustPublicKey(Client client, RequestTrustProcessParameters parameters)
    {
        var recipient = await _cloudSessionsRepository.GetSessionMember(parameters.SessionId, parameters.SessionMemberInstanceId);
        if (recipient != null)
        {
            await _invokeClientsService.Client(recipient).RequestTrustPublicKey(parameters).ConfigureAwait(false);
            
            _logger.LogInformation("RequestTrustPublicKey: {Sender} sends trust publicKey Request to {Recipient}", client.ClientInstanceId,
                parameters.SessionMemberInstanceId);
        }
        else
        { 
            _logger.LogInformation("InformPublicKeyValidationIsFinished: Recipient not found'. Can not proceed");
        }
    }

    public async Task InformPublicKeyValidationIsFinished(Client client, PublicKeyValidationParameters parameters)
    {
        var session = await _cloudSessionsRepository.Get(parameters.SessionId);
        if (session != null)
        {
            await _invokeClientsService.Client(parameters.OtherPartyClientInstanceId).InformPublicKeyValidationIsFinished(parameters).ConfigureAwait(false);
            
            _logger.LogInformation("InformPublicKeyValidationIsFinished: {Sender} sends PublicKeyValidation to {Recipient}", client.ClientInstanceId,
                parameters.OtherPartyClientInstanceId);
        }
        else
        { 
            _logger.LogInformation("AskCloudSessionMembersPublicKeys: session not found for sessionId '{sessionId}'. Can not proceed",
                parameters.SessionId);
        }
    }
}