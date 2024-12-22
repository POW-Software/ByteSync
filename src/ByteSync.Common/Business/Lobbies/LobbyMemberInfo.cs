using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Lobbies;

public class LobbyMemberInfo
{
    public string ProfileClientId { get; set; } = null!;

    public string ClientInstanceId { get; set; } = null!;
    
    public LobbyMemberStatuses Status { get; set; }
    
    public string IpAddress { get; set; } = null!;
    
    public PublicKeyInfo PublicKeyInfo { get; set; } = null!;
    
    public JoinLobbyModes JoinLobbyMode { get; set; }
}