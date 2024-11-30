using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class DigitalSignatureCheckInfo
{
    // public enum IssuerTypes
    // {
    //     Joiner = 1,
    //     SessionMember = 2,
    // }

    public string DataId { get; set; }
    
    public string Issuer { get; set; }
    
    // public IssuerTypes IssuerType { get; set; }
    
    public string Recipient { get; set; }
    
    public byte[] Signature { get; set; }
    
    public PublicKeyInfo PublicKeyInfo { get; set; }
    
    public bool NeedsCrossCheck { get; set; }
}