namespace ByteSync.Common.Business.EndPoints;

public class PublicKeyInfo
{
    public string ClientId { get; set; } = null!;

    public byte[] PublicKey { get; set; } = null!;
    
    public int ProtocolVersion { get; set; }
}