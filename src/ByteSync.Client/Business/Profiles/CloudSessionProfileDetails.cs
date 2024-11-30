using System.Security.Cryptography;

namespace ByteSync.Business.Profiles;

public class CloudSessionProfileDetails : AbstrastSessionProfileDetails
{
    public CloudSessionProfileDetails()
    {
        Options = new CloudSessionProfileOptions();
        
        Members = new List<CloudSessionProfileMember>();
        
        using var aes = Aes.Create();
        aes.BlockSize = 128; 
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 256;
        aes.GenerateKey();

        AesEncryptionKey = aes.Key;
    }

    public override string ProfileId
    {
        get
        {
            return CloudSessionProfileId;
        }
    }

    public string CloudSessionProfileId { get; set; } = null!;
    
    public byte[] AesEncryptionKey { get; }
    
    public CloudSessionProfileOptions Options { get; set; }
    
    public List<CloudSessionProfileMember> Members { get; set; }
}