﻿using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Exceptions;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class UpdateSessionSettingsCommandHandler : IRequestHandler<UpdateSessionSettingsRequest, bool>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<UpdateSessionSettingsCommandHandler> _logger;


    public UpdateSessionSettingsCommandHandler(ICloudSessionsRepository cloudSessionsRepository, IInventoryRepository inventoryRepository, 
        ISynchronizationRepository synchronizationRepository, IRedisInfrastructureService redisInfrastructureService, ISessionMemberMapper sessionMemberMapper, 
        IInvokeClientsService invokeClientsService, ILogger<UpdateSessionSettingsCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }
    
    public async Task<bool> Handle(UpdateSessionSettingsRequest request, CancellationToken cancellationToken)
    {
        if (request.Settings == null)
        {
            throw new BadRequestException("UpdateSessionSettings: sessionSettings null");
        }

        var result = await _cloudSessionsRepository.Update(request.SessionId, cloudSessionData =>
        {
            if (cloudSessionData.IsSessionActivated)
            {
                return false;
            }
            
            cloudSessionData.UpdateSessionSettings(request.Settings);
            
            _logger.LogInformation("UpdateSessionSettings: {cloudSession}", cloudSessionData.SessionId);

            return true;
        });

        if (result.IsSaved)
        {
            var sessionSettingsUpdatedDto = new SessionSettingsUpdatedDTO(request.SessionId, request.Client.ClientInstanceId, request.Settings);
            
            await _invokeClientsService.SessionGroupExcept(request.SessionId, request.Client)
                .SessionSettingsUpdated(sessionSettingsUpdatedDto).ConfigureAwait(false);

            return true;
        }

        return false;
    }
}