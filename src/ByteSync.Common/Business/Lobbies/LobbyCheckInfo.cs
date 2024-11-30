using System.Collections.Generic;

namespace ByteSync.Common.Business.Lobbies;

public class LobbyCheckInfo
{
    public LobbyCheckInfo()
    {
        Recipients = new List<LobbyCheckRecipient>();
    }
    
    public string LobbyId { get; set; } = null!;
    
    public string SenderClientInstanceId { get; set; } = null!;
    
    public List<LobbyCheckRecipient> Recipients { get; set; }
}