namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class InformProtocolVersionIncompatibleParameters
{
    public string SessionId { get; set; } = null!;
    
    public string MemberClientInstanceId { get; set; } = null!;
    
    public string JoinerClientInstanceId { get; set; } = null!;
    
    public int MemberProtocolVersion { get; set; }
    
    public int JoinerProtocolVersion { get; set; }
}

