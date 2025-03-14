using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Business.Sessions;

public class CloudSessionData
{
    public CloudSessionData()
    {
        SessionMembers = new List<SessionMemberData>();

        PreSessionMembers = new List<SessionMemberData>();
    }

    public CloudSessionData(string? lobbyId, EncryptedSessionSettings sessionSettings, Client creator) : this()
    {
        LobbyId = lobbyId;

        SessionSettings = sessionSettings;

        CreatorInstanceId = creator.ClientInstanceId;
    }

    public string SessionId { get; set; } = null!;

    public string? LobbyId { get; set; }
        
    public EncryptedSessionSettings SessionSettings { get; set; } = null!;

    public CloudSessionFatalError? CloudSessionFatalError { get;  private set; }

    public bool IsSessionActivated { get; set; }

    public bool IsSessionOnError
    {
        get
        {
            return CloudSessionFatalError != null;
        }
    }

    public List<SessionMemberData> SessionMembers { get; set; }

    public List<SessionMemberData> PreSessionMembers { get; set;  }

    public string CreatorInstanceId { get; set; }

    public bool IsSessionRemoved { get; set; }

    public bool Contains(ByteSyncEndpoint endpoint)
    {
        return SessionMembers.Any(smd => Equals(smd.ClientInstanceId, endpoint.ClientInstanceId));
    }

    public void SetSessionActivated(EncryptedSessionSettings cloudSessionSettings)
    {
        SessionSettings = cloudSessionSettings;
        IsSessionActivated = true;
    }

    public void UpdateSessionSettings(EncryptedSessionSettings cloudSessionSettings)
    {
        SessionSettings = cloudSessionSettings;
    }

    public CloudSessionFatalError SetSessionOnFatalError(CloudSessionFatalErrors fatalError)
    {
        var cloudSessionFatalError = new CloudSessionFatalError();
        cloudSessionFatalError.SessionId = SessionId;
        cloudSessionFatalError.HappenedOn = DateTimeOffset.UtcNow;
        cloudSessionFatalError.SessionFatalError = fatalError;

        CloudSessionFatalError = cloudSessionFatalError;

        return cloudSessionFatalError;
    }

    public void ResetSession()
    {
        IsSessionActivated = false;
        CloudSessionFatalError = null;
    }

    public CloudSession GetCloudSession()
    {
        var cloudSession = new CloudSession(SessionId, CreatorInstanceId);
        cloudSession.LobbyId = LobbyId;

        return cloudSession;
    }
    
    public SessionMemberData? FindMember(string clientInstanceId)
    {
        return SessionMembers.SingleOrDefault(m => m.ClientInstanceId == clientInstanceId);
    }

    public SessionMemberData? FindMemberOrPreMember(string clientInstanceId)
    {
        return SessionMembers.Concat(PreSessionMembers).Distinct().SingleOrDefault(m => m.ClientInstanceId == clientInstanceId);
    }
}