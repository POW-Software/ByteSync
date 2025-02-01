using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Local;

namespace ByteSync.Interfaces.Controls.Sessions;

public interface ISessionService
{
    public IObservable<AbstractSession?> SessionObservable { get; }
    
    public AbstractSession? CurrentSession { get; }

    public IObservable<SessionSettings?> SessionSettingsObservable { get; }
    
    public SessionSettings? CurrentSessionSettings { get; }
    
    public IObservable<AbstractRunSessionProfileInfo?> RunSessionProfileInfoObservable { get; }
    
    public AbstractRunSessionProfileInfo? CurrentRunSessionProfileInfo { get; }
    
    public IObservable<SessionStatus> SessionStatusObservable { get; }
    
    public SessionStatus CurrentSessionStatus { get; }
    
    public IObservable<bool> HasSessionBeenResettedObservable { get; }
    
    public bool HasSessionBeenResetted { get; }
    
    string? SessionId { get; }
    
    bool IsCloudSession { get; }
    
    bool IsLocalSession { get; }
    
    bool IsProfileSession { get; }
    
    IObservable<bool> SessionEnded { get; }
    
    IObservable<SessionModes?> SessionMode { get; }

    bool IsSessionActivated { get; }
    
    string CloudSessionPassword { get; set; }

    
    void ClearCloudSession();
    
    Task SetCloudSession(CloudSession cloudSession, RunCloudSessionProfileInfo? runCloudSessionProfileInfo, SessionSettings sessionSettings);
    
    Task SetLocalSession(LocalSession localSession, RunLocalSessionProfileInfo? runLocalSessionProfileInfo, SessionSettings sessionSettings);
    
    void SetPassword(string password);
    
    Task SetSessionSettings(SessionSettings sessionSettings);
    
    Task SetSessionStatus(SessionStatus sessionStatus);

    Task ResetSession();
    
    bool CheckSession(string? sessionId);
    
    Task StartLocalSession(RunLocalSessionProfileInfo? runLocalSessionProfileInfo);
}