using System.Security.Cryptography;
using System.Text.Json;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Interfaces.Services;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Services;

public class AuthenticationChallengeService(IRedisInfrastructureService redis) : IAuthenticationChallengeService
{
    private sealed record ChallengeRecord(LoginData Login, AuthenticationChallenge Challenge);

    private const string ConsumeChallengeScript = """
        local value = redis.call('GET', KEYS[1])
        if value then
            redis.call('DEL', KEYS[1])
        end
        return value
        """;

    public async Task<AuthenticationChallenge> Create(LoginData login)
    {
        Validate(login);
        var challenge = new AuthenticationChallenge
        {
            Id = Guid.NewGuid().ToString("N"),
            Nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(2)
        };
        var stored = await redis.GetDatabase().StringSetAsync(Key(challenge.Id),
            JsonSerializer.Serialize(new ChallengeRecord(login, challenge)), TimeSpan.FromMinutes(2), When.NotExists);
        if (!stored) throw new InvalidOperationException("Challenge could not be created");
        return challenge;
    }

    public async Task<bool> VerifyAndConsume(LoginData login)
    {
        try
        {
            Validate(login);
            if (!Guid.TryParseExact(login.ChallengeId, "N", out _)) return false;
            var value = (RedisValue)await redis.GetDatabase().ScriptEvaluateAsync(ConsumeChallengeScript,
                new RedisKey[] { Key(login.ChallengeId) }, Array.Empty<RedisValue>());
            if (value.IsNullOrEmpty) return false;
            var record = JsonSerializer.Deserialize<ChallengeRecord>((string)value!)!;
            if (record.Challenge.ExpiresAt <= DateTimeOffset.UtcNow) return false;
            var message = AuthenticationProtocol.GetMessage(login, record.Challenge);
            if (!message.AsSpan().SequenceEqual(AuthenticationProtocol.GetMessage(record.Login, record.Challenge))) return false;
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(login.PublicKey, out _);
            return rsa.VerifyData(message, login.Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException or JsonException)
        {
            return false;
        }
    }

    private static string Key(string id) => $"auth:{AuthenticationProtocol.Epoch}:challenge:{id}";

    private static void Validate(LoginData login)
    {
        if (login.PublicKey == null || login.PublicKey.Length > 2048 || login.Signature == null || login.Signature.Length > 1024 ||
            login.AuthenticationEpoch != AuthenticationProtocol.Epoch || login.ProtocolVersion != ProtocolVersion.CURRENT ||
            string.IsNullOrEmpty(login.Version) || login.Version.Length > 100 ||
            AuthenticationProtocol.GetIdentity(login.PublicKey) != login.Identity ||
            !AuthenticationProtocol.IsInstanceOf(login.ClientInstanceId, login.Identity))
            throw new CryptographicException("Invalid authentication identity");
    }
}
