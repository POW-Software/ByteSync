using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class TrustService : ITrustService
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ILobbyRepository _lobbyRepository;
    private readonly IClientsGroupsInvoker _clientsGroupsInvoker;
    private readonly ILogger<TrustService> _logger;


    public TrustService(ICloudSessionsRepository cloudSessionsRepository, ILobbyRepository lobbyRepository, 
        IClientsGroupsInvoker clientsGroupsInvoker, ILogger<TrustService> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _lobbyRepository = lobbyRepository;
        _clientsGroupsInvoker = clientsGroupsInvoker;
        _logger = logger;
    }
    
    public async Task<StartTrustCheckResult> StartTrustCheck(Client joiner, TrustCheckParameters trustCheckParameters)
    {
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
                
                await _clientsGroupsInvoker.Client(clientInstanceId).AskPublicKeyCheckData(trustCheckParameters.SessionId, joiner.ClientInstanceId,
                    trustCheckParameters.PublicKeyInfo).ConfigureAwait(false);
            }
        }
        
        return new StartTrustCheckResult { IsOK = true, MembersInstanceIds = members };
    }

    public async Task SendDigitalSignatures(Client client, SendDigitalSignaturesParameters parameters)
    {
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
                await _clientsGroupsInvoker.Client(digitalSignatureCheckInfo.Recipient).RequestCheckDigitalSignature(digitalSignatureCheckInfo).ConfigureAwait(false);
            }
        }
        else
        {
            _logger.LogInformation("SendDigitalSignatures: session or lobby not found for Id '{dataId}'. Can not proceed", parameters.DataId);
        }
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
            await _clientsGroupsInvoker.Client(recipient).RequestTrustPublicKey(parameters).ConfigureAwait(false);
            
            _logger.LogInformation("RequestTrustPublicKey: {Sender} sends trust publicKey Request to {Recipient}", client.ClientInstanceId,
                parameters.SessionMemberInstanceId);
        }
        else
        { 
            _logger.LogInformation("InformPublicKeyValidationIsFinished: Recipient not found'. Can not proceed");
        }
    }

    public async Task GiveMemberPublicKeyCheckData(Client client, GiveMemberPublicKeyCheckDataParameters parameters)
    {
        await _clientsGroupsInvoker.Client(parameters.ClientInstanceId).GiveMemberPublicKeyCheckData(parameters.SessionId, parameters.PublicKeyCheckData).ConfigureAwait(false);
            
        _logger.LogInformation("GiveMemberPublicKeyCheckData: {Sender} gives PublicKeyCheckData to {Recipient}", client.ClientInstanceId,
            parameters.ClientInstanceId);
    }

    public async Task InformPublicKeyValidationIsFinished(Client client, PublicKeyValidationParameters parameters)
    {
        var session = await _cloudSessionsRepository.Get(parameters.SessionId);
        if (session != null)
        {
            await _clientsGroupsInvoker.Client(parameters.OtherPartyClientInstanceId).InformPublicKeyValidationIsFinished(parameters).ConfigureAwait(false);
            
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