using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class UpdateSessionSettingsCommandHandler : IRequestHandler<UpdateSessionSettingsRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IByteSyncClientCaller _byteSyncClientCaller;
    private readonly ILogger<UpdateSessionSettingsCommandHandler> _logger;


    public UpdateSessionSettingsCommandHandler(ICloudSessionsRepository cloudSessionsRepository, IInventoryRepository inventoryRepository, 
        ISynchronizationRepository synchronizationRepository, ICacheService cacheService, ISessionMemberMapper sessionMemberMapper, 
        IByteSyncClientCaller byteSyncClientCaller, ILogger<UpdateSessionSettingsCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _byteSyncClientCaller = byteSyncClientCaller;
        _logger = logger;
    }
    
    public async Task Handle(UpdateSessionSettingsRequest request, CancellationToken cancellationToken)
    {
        if (request.Settings == null)
        {
            _logger.LogWarning("UpdateSessionSettings: sessionSettings null");
            return;
        }

        var result = await _cloudSessionsRepository.Update(request.SessionId, cloudSessionData =>
        {
            cloudSessionData.UpdateSessionSettings(request.Settings);
            
            _logger.LogInformation("UpdateSessionSettings: {cloudSession}", cloudSessionData.SessionId);

            return true;
        });

        if (result.IsSaved)
        {
            var sessionSettingsUpdatedDto = new SessionSettingsUpdatedDTO(request.SessionId, request.Client.ClientInstanceId, request.Settings);
            
            await _byteSyncClientCaller.SessionGroupExcept(request.SessionId, request.Client).SessionSettingsUpdated(sessionSettingsUpdatedDto).ConfigureAwait(false);
        }
    }
}