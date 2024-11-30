using System.Collections.Generic;
using System.Linq;

namespace ByteSync.Common.Business.Lobbies;

public class LobbyInfo
{
    public string LobbyId { get; set; }
    
    public string CloudSessionProfileId { get; set; }
    
    public string ProfileDetailsPassword { get; set; }
    
    public List<LobbyMemberInfo> ConnectedMembers { get; set; }

    public LobbyMemberInfo? GetMember(string profileClientId)
    {
        return ConnectedMembers.SingleOrDefault(cm => cm.ProfileClientId.Equals(profileClientId));
    }
}