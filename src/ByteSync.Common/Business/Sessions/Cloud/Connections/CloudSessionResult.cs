using System.Collections.Generic;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class CloudSessionResult
{
    public CloudSessionResult()
    {
        MembersIds = new List<string>();
    }

    public CloudSessionResult(CloudSession cloudSession, EncryptedSessionSettings sessionSettings, SessionMemberInfoDTO sessionMemberInfo)
        : this()
    {
        CloudSession = cloudSession;
        SessionSettings = sessionSettings;
        SessionMemberInfo = sessionMemberInfo;
    }

    public CloudSession CloudSession { get; set; }
        
    public EncryptedSessionSettings SessionSettings { get; set; }

    public SessionMemberInfoDTO SessionMemberInfo { get; set; }

    public List<string> MembersIds { get; set; }

    public string SessionId
    {
        get
        {
            return CloudSession.SessionId;
        }
    }
}