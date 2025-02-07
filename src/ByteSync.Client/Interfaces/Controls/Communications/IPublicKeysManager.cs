using System.Collections.ObjectModel;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IPublicKeysManager
{
    PublicKeyInfo GetMyPublicKeyInfo();
    
    PublicKeyCheckData BuildJoinerPublicKeyCheckData(PublicKeyCheckData memberPublicKeyCheckData);
    
    PublicKeyCheckData BuildMemberPublicKeyCheckData(PublicKeyInfo joinerPublicKeyInfo, bool isTrustedByMember);

    bool IsTrusted(PublicKeyCheckData publicKeyCheckData);
    
    bool IsTrusted(PublicKeyInfo publicKeyInfo);
    
    ReadOnlyCollection<TrustedPublicKey>? GetTrustedPublicKeys();

    TrustedPublicKey BuildTrustedPublicKey(PublicKeyCheckData publicKeyCheckData);
    
    void Trust(TrustedPublicKey trustedPublicKey);
    
    void Delete(TrustedPublicKey trustedPublicKey);
    
    void InitializeRsaAndTrustedPublicKeys();
    
    byte[] DecryptBytes(byte[] messageToEncrypt);
    
    string DecryptString(byte[] messageToEncrypt);
    
    byte[] EncryptBytes(PublicKeyInfo publicKeyInfo, byte[] messageToEncrypt);

    byte[] EncryptString(PublicKeyInfo publicKeyInfo, string messageToEncrypt);

    byte[] SignData(string dataToEncrypt);

    bool VerifyData(PublicKeyInfo publicKeyInfo, byte[] dataToVerify, string signature);
}