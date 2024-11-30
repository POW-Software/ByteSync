using System;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Common.Business.Sessions;

public abstract class AbstractSession
{
    protected AbstractSession()
    {
        Created = DateTimeOffset.UtcNow;
    }

    protected AbstractSession(string sessionId, string creatorInstanceId) : this()
    {
        SessionId = sessionId;
        CreatorInstanceId = creatorInstanceId;
    }

    public string SessionId { get; set; }
    
    public string CreatorInstanceId { get; set; }
    
    public DateTimeOffset Created { get; set; }
    
    protected bool Equals(AbstractSession other)
    {
        return SessionId == other.SessionId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AbstractSession)obj);
    }

    public override int GetHashCode()
    {
        return SessionId.GetHashCode();
    }
}