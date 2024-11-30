using System;

namespace ByteSync.Common.Business.Sessions.Cloud;

public class CloudSession : AbstractSession
{
    public CloudSession()
    {
        VersionNumber = 0;
    }

    public CloudSession(string sessionId, string creatorInstanceId)
        : base(sessionId, creatorInstanceId)
    {

    }

    public int VersionNumber { get; set; }

    public string? LobbyId { get; set; }

    public void IncrementVersionNumber()
    {
        VersionNumber += 1;
    }
}