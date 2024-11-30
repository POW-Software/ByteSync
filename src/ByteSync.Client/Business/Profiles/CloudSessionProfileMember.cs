namespace ByteSync.Business.Profiles;

public class CloudSessionProfileMember
{
    public CloudSessionProfileMember()
    {
        PathItems = new List<SessionProfilePathItem>();
    }
    
    public string MachineName { get; set; }
    
    public string IpAddress { get; set; }
    
    public string ProfileClientId { get; set; }
    
    public string ProfileClientPassword { get; set; }
    
    public List<SessionProfilePathItem> PathItems { get; set; }
    
    public string Letter { get; set; }
}