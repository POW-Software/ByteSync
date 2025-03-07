using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class CreateSessionCommandHandler : IRequestHandler<CreateSessionRequest, CloudSessionResult>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IClientsGroupsService _clientsGroupsService;
    private readonly ICloudSessionsService _cloudSessionsService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateSessionCommandHandler> _logger;

    public CreateSessionCommandHandler(ICloudSessionsRepository cloudSessionsRepository, IClientsGroupsService clientsGroupsService,
        IClientsRepository clientsRepository, ICloudSessionsService cloudSessionsService, ICacheService cacheService, 
        ILogger<CreateSessionCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _clientsGroupsService = clientsGroupsService;
        _cloudSessionsService = cloudSessionsService;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    public async Task<CloudSessionResult> Handle(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var createCloudSessionParameters = request.CreateCloudSessionParameters;
        var client = request.Client;
        
        var transaction = _cacheService.OpenTransaction();
        
        CloudSessionData cloudSessionData;
        SessionMemberData creatorData;
        
        cloudSessionData = new CloudSessionData(createCloudSessionParameters.LobbyId, createCloudSessionParameters.SessionSettings, client);
        creatorData = new SessionMemberData(client, createCloudSessionParameters.CreatorPublicKeyInfo, 
            createCloudSessionParameters.CreatorProfileClientId, cloudSessionData, 
            createCloudSessionParameters.CreatorPrivateData);
        cloudSessionData.SessionMembers.Add(creatorData);

        cloudSessionData = await _cloudSessionsRepository.AddCloudSession(cloudSessionData, GenerateRandomSessionId, transaction);
        
        await _clientsGroupsService.AddSessionSubscription(client, cloudSessionData.SessionId, transaction);

        await transaction.ExecuteAsync();

        await _clientsGroupsService.AddToSessionGroup(client, cloudSessionData.SessionId);

        _logger.LogInformation("Cloud Session {SessionId} created", cloudSessionData.SessionId);

        var cloudSessionResult = await _cloudSessionsService.BuildCloudSessionResult(cloudSessionData, creatorData);

        return cloudSessionResult;
    }
    
    private string GenerateRandomSessionId()
    {
        string sessionId = RandomUtils.GetRandomNumber(3) + 
                           RandomUtils.GetRandomLetters(3, false) +
                           RandomUtils.GetRandomNumber(3);

        return sessionId;
    }
}