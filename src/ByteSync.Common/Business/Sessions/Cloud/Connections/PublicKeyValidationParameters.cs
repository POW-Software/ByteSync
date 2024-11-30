namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class PublicKeyValidationParameters
{
    public PublicKeyValidationParameters()
    {
        
    }
    
    public string SessionId { get; set; }
    
    public string IssuerClientId { get; set; }
    
    public string OtherPartyClientInstanceId { get; set; }
    
    public bool IsValidated { get; set; }
    
    public byte[] OtherPartyPublicKey { get; set; }
}