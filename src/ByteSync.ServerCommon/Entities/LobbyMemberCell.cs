namespace ByteSync.ServerCommon.Entities;

public class LobbyMemberCell
{
    public LobbyMemberCell(string profileClientId)
    {
        ProfileClientId = profileClientId;

        LobbyMember = null;
    }
    
    public string ProfileClientId { get; }
    
    public LobbyMember? LobbyMember { get; set; }

    public bool IsMemberSet
    {
        get
        {
            return LobbyMember != null;
        }
    }
}