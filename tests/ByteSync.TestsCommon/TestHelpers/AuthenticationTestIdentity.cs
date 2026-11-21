using System.Security.Cryptography;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Versions;

namespace ByteSync.TestsCommon.TestHelpers;

public sealed class AuthenticationTestIdentity : IDisposable
{
    private readonly RSA _key = RSA.Create(2048);

    public AuthenticationTestIdentity()
    {
        Identity = AuthenticationProtocol.GetIdentity(_key.ExportRSAPublicKey());
        ClientInstanceId = $"{Identity}_{Guid.NewGuid():D}";
    }

    public string Identity { get; }
    public string ClientInstanceId { get; }

    public LoginData CreateLogin() => new()
    {
        ClientId = Identity,
        Identity = Identity,
        ClientInstanceId = ClientInstanceId,
        PublicKey = _key.ExportRSAPublicKey(),
        AuthenticationEpoch = AuthenticationProtocol.Epoch,
        ProtocolVersion = ProtocolVersion.CURRENT,
        Version = "9.9.9",
        OsPlatform = OSPlatforms.Windows
    };

    public PublicKeyInfo CreatePublicKeyInfo() => new()
    {
        ClientId = Identity,
        PublicKey = _key.ExportRSAPublicKey(),
        ProtocolVersion = ProtocolVersion.CURRENT
    };

    public void Sign(LoginData login, AuthenticationChallenge challenge)
    {
        login.ChallengeId = challenge.Id;
        login.Signature = _key.SignData(AuthenticationProtocol.GetMessage(login, challenge),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    public void Dispose() => _key.Dispose();
}
