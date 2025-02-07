using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Business.Sessions.Connecting;

public class JoinSessionError
{
    public Exception? Exception { get; set; }
    
    public JoinSessionStatus Status { get; set; }
}