using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.PushReceivers;

public class SynchronizationProgressPushReceiver : IPushReceiver
{
    private readonly IHubPushHandler2 _hubPushHandler2;
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    private readonly ISynchronizationApiClient _synchronizationApiClient;
    private readonly ILogger<SynchronizationProgressPushReceiver> _logger;

    public SynchronizationProgressPushReceiver(IHubPushHandler2 hubPushHandler2, ISessionService sessionService,
        ISynchronizationService synchronizationService, ISharedActionsGroupRepository sharedActionsGroupRepository, 
        ISynchronizationApiClient synchronizationApiClient,
        ILogger<SynchronizationProgressPushReceiver> logger)
    {
        _hubPushHandler2 = hubPushHandler2;
        _sessionService = sessionService;
        _synchronizationService = synchronizationService;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _synchronizationApiClient = synchronizationApiClient;
        _logger = logger;
        
        _hubPushHandler2.SynchronizationProgressUpdated.Subscribe(SynchronizationProgressChanged);
    }

    private async void SynchronizationProgressChanged(SynchronizationProgressPush synchronizationProgressPush)
    {
        string? sessionId = null;
        try
        {
            sessionId = _sessionService.CurrentSession?.SessionId;
            if (sessionId == null)
            {
                _logger.LogWarning("Received a synchronization progress push but there is no current session");
                return;
            }

            if (sessionId != synchronizationProgressPush.SessionId)
            {
                _logger.LogWarning("Received a synchronization progress push for a different session than the current one");
                return;
            }

            await _synchronizationService.OnSynchronizationProgressChanged(synchronizationProgressPush);

            await _sharedActionsGroupRepository.OnSynchronizationProgressChanged(synchronizationProgressPush);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing synchronization progress push");

            if (sessionId != null && synchronizationProgressPush.TrackingActionSummaries != null)
            {
                try
                {
                    await _synchronizationApiClient.AssertSynchronizationActionErrors(sessionId!,
                        synchronizationProgressPush.TrackingActionSummaries.Select(tas => tas.ActionsGroupId).ToList());
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Error asserting synchronization action errors");
                }
            }
        }
    }
}