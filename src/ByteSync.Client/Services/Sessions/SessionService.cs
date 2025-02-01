using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business;
using ByteSync.Business.Navigations;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Local;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;

namespace ByteSync.Services.Sessions;

public class SessionService : ISessionService
{
    private readonly ICloudProxy _connectionManager;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly INavigationService _navigationService;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ILogger<SessionService> _logger;
    
    private readonly BehaviorSubject<AbstractSession?> _session;
    private readonly BehaviorSubject<SessionSettings?> _sessionSettings;
    private readonly BehaviorSubject<AbstractRunSessionProfileInfo?> _runSessionProfileInfo;
    private readonly BehaviorSubject<SessionStatus> _sessionStatus;
    private readonly BehaviorSubject<bool> _hasSessionBeenReset;
    private readonly BehaviorSubject<SessionModes?> _sessionMode;
    
    public SessionService(ICloudProxy connectionManager, IDataEncrypter dataEncrypter,
        INavigationService navigationService, ICloudSessionApiClient cloudSessionApiClient,
        ILogger<SessionService> logger)
    {
        _connectionManager = connectionManager;
        _dataEncrypter = dataEncrypter;
        _navigationService = navigationService;
        _cloudSessionApiClient = cloudSessionApiClient;
        _logger = logger;
        
        _session = new BehaviorSubject<AbstractSession?>(null);
        _sessionSettings = new BehaviorSubject<SessionSettings?>(null);
        _runSessionProfileInfo = new BehaviorSubject<AbstractRunSessionProfileInfo?>(null);
        _sessionStatus = new BehaviorSubject<SessionStatus>(SessionStatus.None);
        _hasSessionBeenReset = new BehaviorSubject<bool>(false);
        _sessionMode = new BehaviorSubject<SessionModes?>(SessionModes.Cloud);

        _connectionManager.HubPushHandler2.SessionSettingsUpdated
            .Where(dto => CheckSession(dto.SessionId))
            .Subscribe(dto =>
            {
                var sessionSettings = _dataEncrypter.DecryptSessionSettings(dto.EncryptedSessionSettings);
                _sessionSettings.OnNext(sessionSettings);
            });
    }
    
    public IObservable<AbstractSession?> SessionObservable => _session.AsObservable();
    
    public AbstractSession? CurrentSession => _session.Value;

    public IObservable<SessionSettings?> SessionSettingsObservable => _sessionSettings.AsObservable();
    
    public SessionSettings? CurrentSessionSettings  => _sessionSettings.Value;

    public IObservable<AbstractRunSessionProfileInfo?> RunSessionProfileInfoObservable => _runSessionProfileInfo.AsObservable();
    
    public AbstractRunSessionProfileInfo? CurrentRunSessionProfileInfo => _runSessionProfileInfo.Value;
    
    public IObservable<SessionStatus> SessionStatusObservable => _sessionStatus.AsObservable();
    
    public SessionStatus CurrentSessionStatus => _sessionStatus.Value;
    
    public IObservable<bool> HasSessionBeenResettedObservable => _hasSessionBeenReset.AsObservable();

    public bool HasSessionBeenResetted => _hasSessionBeenReset.Value;
    
    public IObservable<bool> SessionEnded => 
        SessionStatusObservable.Select(ss => ss.In(SessionStatus.FatalError, SessionStatus.RegularEnd));
    
    public IObservable<SessionModes?> SessionMode => _sessionMode.AsObservable();
    
    public string? SessionId => CurrentSession?.SessionId;

    public bool IsCloudSession => CurrentSession is CloudSession;

    public bool IsLocalSession => CurrentSession is LocalSession;

    public bool IsProfileSession => CurrentRunSessionProfileInfo != null;

    public bool IsSessionActivated
    {
        get
        {
            return CurrentSessionStatus.In(SessionStatus.Inventory, SessionStatus.Synchronization);
        }
    }

    public string? CloudSessionPassword { get; set; }
    
    public bool CheckSession(string? sessionId)
    {
        var isOK = sessionId.IsNotEmpty(true) && sessionId!.Equals(SessionId);
        
        return isOK;
    }

    public async Task StartLocalSession(RunLocalSessionProfileInfo? runLocalSessionProfileInfo)
    {
        var sessionId = $"LSID_{Guid.NewGuid()}";
        var localSession = new LocalSession(sessionId, _connectionManager.CurrentEndPoint.ClientInstanceId);

        var cloudSessionSettings = SessionSettings.BuildDefault();
                
        await SetLocalSession(localSession, runLocalSessionProfileInfo, cloudSessionSettings);
                
        _logger.LogInformation("Starting Local Session {SessionId}", sessionId);
                
        _navigationService.NavigateTo(NavigationPanel.LocalSynchronization);
    }

    public void ClearCloudSession()
    {
        _session.OnNext(null);
        _sessionSettings.OnNext(null);
        _runSessionProfileInfo.OnNext(null);
        _sessionStatus.OnNext(SessionStatus.None);
    }

    public Task SetCloudSession(CloudSession cloudSession, RunCloudSessionProfileInfo? runCloudSessionProfileInfo,
        SessionSettings sessionSettings)
    {
        return Task.Run(() =>
        {
            _session.OnNext(cloudSession);

            _runSessionProfileInfo.OnNext(runCloudSessionProfileInfo);

            _sessionSettings.OnNext(sessionSettings);
            
            _sessionStatus.OnNext(SessionStatus.Preparation);
            
            _hasSessionBeenReset.OnNext(false);
        });
    }
    
    public Task SetLocalSession(LocalSession localSession, RunLocalSessionProfileInfo? runLocalSessionProfileInfo,
        SessionSettings sessionSettings)
    {
        return Task.Run(() =>
        {
            _session.OnNext(localSession);

            _runSessionProfileInfo.OnNext(runLocalSessionProfileInfo);

            _sessionSettings.OnNext(sessionSettings);
            
            _sessionStatus.OnNext(SessionStatus.Preparation);
            
            _hasSessionBeenReset.OnNext(false);
        });
    }

    public void SetPassword(string password)
    {
        CloudSessionPassword = password;
    }

    public async Task SetSessionSettings(SessionSettings sessionSettings)
    {
        var currentSession = CurrentSession;
        
        if (currentSession is CloudSession)
        {
            var encryptedSessionSettings = _dataEncrypter.EncryptSessionSettings(sessionSettings);
                    
            await _cloudSessionApiClient.UpdateSettings(currentSession.SessionId, encryptedSessionSettings);
        }

        _sessionSettings.OnNext(sessionSettings);
    }

    public Task SetSessionStatus(SessionStatus sessionStatus)
    {
        return Task.Run(() => _sessionStatus.OnNext(sessionStatus));
    }

    public Task ResetSession()
    {
        return Task.Run(() =>
        {
            _sessionStatus.OnNext(SessionStatus.Preparation);

            _hasSessionBeenReset.OnNext(true);
        });
    }
}