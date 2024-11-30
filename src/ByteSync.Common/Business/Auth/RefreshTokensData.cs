using ByteSync.Common.Business.Misc;

namespace ByteSync.Common.Business.Auth;

public class RefreshTokensData
{
    public string Token { get; set; }

    public string ClientInstanceId { get; set; }
    
    public string Machinename { get; set; }
    
    public string Version { get; set; }
    
    public OSPlatforms? OsPlatform { get; set; }
}