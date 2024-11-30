namespace ByteSync.Common.Business.Sessions.Local;

public class LocalSession : AbstractSession
{
    public LocalSession()
    {

    }

    public LocalSession(string sessionId, string creatorInstanceId)
        : base(sessionId, creatorInstanceId)
    {

    }
}