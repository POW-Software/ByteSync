namespace ByteSync.Common.Business.Lobbies;

public class LobbyCheckRecipient
{
    public string ClientInstanceId { get; set; } = null!;

    public byte[] CheckData { get; set; } = null!;
}