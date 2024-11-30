namespace ByteSync.Common.Business.Lobbies;

public class LobbyCloudSessionCredentials
{
    public string LobbyId { get; set; }
    
    public byte[] Info { get; set; }
    
    public string Recipient { get; set; }
}