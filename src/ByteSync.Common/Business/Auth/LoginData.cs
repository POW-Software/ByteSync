using ByteSync.Common.Business.Misc;

namespace ByteSync.Common.Business.Auth;

public class LoginData
{
    public string ClientId { get; set; }
    
    public string ClientInstanceId { get; set; }
    
    public string Version { get; set; }
    
    public OSPlatforms? OsPlatform { get; set; }
}