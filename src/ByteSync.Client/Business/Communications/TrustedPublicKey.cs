namespace ByteSync.Business.Communications;

public class TrustedPublicKey
{
    public TrustedPublicKey()
    {
        
    }
    
    public string ClientId { get; set; } = null!;

    public string SafetyKey { get; set; } = null!;
    
    public byte[] PublicKey { get; set; } = null!;
    
    public string PublicKeyHash { get; set; } = null!;

    public DateTimeOffset ValidationDate { get; set; }
}