using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Lobbies.Connections;

public class JoinLobbyParameters
{
    public string CloudSessionProfileId { get; set; } 
    
    public string ProfileClientId { get; set; }
    
    public PublicKeyInfo PublicKeyInfo { get; set; }
    
    public JoinLobbyModes JoinMode { get; set; }
}