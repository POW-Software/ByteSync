using ByteSync.Common.Business.Misc;

namespace ByteSync.Common.Business.Auth;

public class LoginData
{
    public int AuthenticationEpoch { get; set; }
    public int ProtocolVersion { get; set; }
    public string Identity { get; set; } = "";
    public byte[] PublicKey { get; set; } = [];
    public string ChallengeId { get; set; } = "";
    public byte[] Signature { get; set; } = [];

    public string ClientId { get; set; }
    
    public string ClientInstanceId { get; set; }
    
    public string Version { get; set; }
    
    public OSPlatforms? OsPlatform { get; set; }
}
