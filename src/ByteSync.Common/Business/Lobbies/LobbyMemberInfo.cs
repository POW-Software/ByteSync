using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Lobbies;

public class LobbyMemberInfo
{
    public string ProfileClientId { get; set; }
    
    public string ClientInstanceId { get; set; }
    
    public LobbyMemberStatuses Status { get; set; }
    
    public string IpAddress { get; set; }
    
    public PublicKeyInfo PublicKeyInfo { get; set; }
    
    public JoinLobbyModes JoinLobbyMode { get; set; }
}