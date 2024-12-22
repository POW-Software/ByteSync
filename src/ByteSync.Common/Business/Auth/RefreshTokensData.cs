using ByteSync.Common.Business.Misc;

namespace ByteSync.Common.Business.Auth;

public class RefreshTokensData
{
    public string Token { get; set; } = null!;

    public string ClientInstanceId { get; set; } = null!;
    
    public string Version { get; set; } = null!;
    
    public OSPlatforms? OsPlatform { get; set; }
}