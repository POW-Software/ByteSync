using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ByteSync.Common.Business.Versions;

namespace ByteSync.Common.Business.Auth;

public static class AuthenticationProtocol
{
    public const int Epoch = 1;
    public const string Audience = "ByteSync.Relay";

    public static string GetIdentity(byte[] publicKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKey, out var read);
        if (read != publicKey.Length || rsa.KeySize < 2048 || rsa.KeySize > 8192)
            throw new CryptographicException("Invalid public key");
        return Convert.ToHexString(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()));
    }

    public static bool IsInstanceOf(string? instanceId, string identity)
    {
        return identity.Length == 64 && identity.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F') &&
               instanceId != null && instanceId.Length == identity.Length + 37 &&
               instanceId.StartsWith(identity + "_", StringComparison.Ordinal) &&
               Guid.TryParseExact(instanceId.Substring(identity.Length + 1), "D", out _);
    }

    public static byte[] GetMessage(LoginData login, AuthenticationChallenge challenge)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write("ByteSync.Login");
        writer.Write(Epoch);
        writer.Write(Audience);
        writer.Write(ProtocolVersion.CURRENT);
        writer.Write(challenge.Id);
        writer.Write(challenge.Nonce);
        writer.Write(challenge.ExpiresAt.UtcTicks);
        writer.Write(login.Identity);
        writer.Write(login.ClientInstanceId);
        writer.Write(login.Version);
        writer.Write((int)(login.OsPlatform ?? 0));
        return stream.ToArray();
    }
}
