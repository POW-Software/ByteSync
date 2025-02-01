using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions.Connecting;

class CloudSessionConnector : ICloudSessionConnector
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<CloudSessionConnector>();
    
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationService _synchronizationService;
    
    public CloudSessionConnector(ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ICloudSessionEventsHub cloudSessionEventsHub, IDigitalSignaturesRepository digitalSignaturesRepository, 
        ITrustProcessPublicKeysRepository trustPublicKeysRepository, ISessionService sessionService, ISynchronizationService synchronizationService)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _cloudSessionEventsHub = cloudSessionEventsHub;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _trustProcessPublicKeysRepository = trustPublicKeysRepository;
        _sessionService = sessionService;
        _synchronizationService = synchronizationService;
    }
    
    public async Task ClearConnectionData()
    {
        await Task.WhenAll(
            _cloudSessionConnectionRepository.ClearAsync(), 
            _trustProcessPublicKeysRepository.ClearAsync(), 
            _digitalSignaturesRepository.ClearAsync());
    }

    public IObservable<bool> CanLogOutOrShutdown 
    {
        get
        {
            return Observable.CombineLatest(_cloudSessionConnectionRepository.ConnectionStatusObservable,
                _sessionService.SessionObservable, _synchronizationService.SynchronizationProcessData.SynchronizationEnd, 
                _sessionService.SessionStatusObservable,
                (connectionStatus, session, synchronizationEnd, sessionStatus) =>
                    !connectionStatus.In(SessionConnectionStatus.CreatingSession, SessionConnectionStatus.JoiningSession) &&
                    (session == null || synchronizationEnd != null)
                    && sessionStatus.In(SessionStatus.FatalError, SessionStatus.None, SessionStatus.RegularEnd));
        }
    }

    public async Task InitializeConnection(SessionConnectionStatus sessionConnectionStatus)
    {
        await ClearConnectionData();
        _cloudSessionConnectionRepository.SetConnectionStatus(sessionConnectionStatus);

        _cloudSessionConnectionRepository.CancellationTokenSource = new CancellationTokenSource();

        if (sessionConnectionStatus == SessionConnectionStatus.CreatingSession)
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            _cloudSessionConnectionRepository.SetAesEncryptionKey(aes.Key);
        }

        if (sessionConnectionStatus != SessionConnectionStatus.InSession)
        {
            _sessionService.ClearCloudSession();
        }
    }
    
    public async Task OnJoinSessionError(JoinSessionResult joinSessionResult)
    {
        _cloudSessionConnectionRepository.SetConnectionStatus(SessionConnectionStatus.NoSession);
        await ClearConnectionData();
            
        Log.Error("Can not join the Cloud Session. Reason: {Reason}", joinSessionResult.Status);
        await _cloudSessionEventsHub.RaiseJoinCloudSessionFailed(joinSessionResult);
    }
}