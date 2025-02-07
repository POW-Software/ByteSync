using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.PushReceivers;

public class SynchronizationPushReceiver : IPushReceiver
{
    private readonly IHubPushHandler2 _hubPushHandler2;
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<SynchronizationPushReceiver> _logger;


    public SynchronizationPushReceiver(IHubPushHandler2 hubPushHandler2, ISessionService sessionService, ISynchronizationService synchronizationService,
        ILogger<SynchronizationPushReceiver> logger)
    {
        _hubPushHandler2 = hubPushHandler2;
        _sessionService = sessionService;   
        _synchronizationService = synchronizationService;
        _logger = logger;
        
        AbstractSession? latestSession = null;
        _sessionService.SessionObservable.Subscribe(value => latestSession = value); 
        
        _hubPushHandler2.SynchronizationStarted
            .Subscribe(synchronization =>
            {
                if (latestSession != null && latestSession.SessionId.Equals(synchronization.SessionId))
                {
                    _logger.LogInformation("The Data Synchronization has been started by another client ({@StartedBy}). " +
                                           "Retrieving data...", synchronization.StartedBy);
                    
                    _synchronizationService.OnSynchronizationStarted(synchronization);
                }
                else
                {
                    // sessionId is not expected, how to deal with that?
                }
            });
        
        _hubPushHandler2.SynchronizationUpdated
            .Subscribe(synchronization =>
            {
                if (latestSession != null && latestSession.SessionId.Equals(synchronization.SessionId))
                {
                    _logger.LogInformation("The Data Synchronization has been updated");

                    _synchronizationService.OnSynchronizationUpdated(synchronization);
                }
                else
                {
                    // sessionId is not expected, how to deal with that?
                }
            });
    }
}