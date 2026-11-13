using System.Security.Cryptography;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Integration;

[Explicit("Requires a disposable Redis 6.0 bound to 127.0.0.1 and the Redis60Port test parameter")]
public class AuthenticationChallengeRedis60Tests
{
    private ConnectionMultiplexer _connection = null!;
    private IDatabase _database = null!;
    private AuthenticationChallengeService _service = null!;
    private RSA _key = null!;
    private int _port;
    private readonly List<RedisKey> _challengeKeys = new();

    [OneTimeSetUp]
    public async Task ConnectToDisposableLocalRedis()
    {
        if (!int.TryParse(TestContext.Parameters.Get("Redis60Port", ""), out _port) || _port is < 1 or > 65535)
            Assert.Ignore("Supply Redis60Port for a disposable local Redis 6.0 instance");
        _connection = await ConnectionMultiplexer.ConnectAsync(Options());
        var version = _connection.GetServer("127.0.0.1", _port).Version;
        version.Major.Should().Be(6);
        version.Minor.Should().Be(0);
        _database = _connection.GetDatabase();
    }

    private ConfigurationOptions Options() => new()
    {
        EndPoints = { { "127.0.0.1", _port } },
        AbortOnConnectFail = true, ConnectRetry = 0, ConnectTimeout = 5000, SyncTimeout = 5000
    };

    private static AuthenticationChallengeService Service(IDatabase database)
    {
        var redis = A.Fake<IRedisInfrastructureService>();
        A.CallTo(() => redis.GetDatabase()).Returns(database);
        return new AuthenticationChallengeService(redis);
    }

    [SetUp]
    public void SetUp()
    {
        _key = RSA.Create(2048);
        _service = Service(_database);
        _challengeKeys.Clear();
    }

    private LoginData Login()
    {
        var identity = AuthenticationProtocol.GetIdentity(_key.ExportRSAPublicKey());
        return new LoginData { Identity = identity, ClientInstanceId = identity + "_" + Guid.NewGuid().ToString("D"),
            PublicKey = _key.ExportRSAPublicKey(), AuthenticationEpoch = AuthenticationProtocol.Epoch,
            ProtocolVersion = ProtocolVersion.CURRENT, Version = "local-redis60-test" };
    }

    private async Task<AuthenticationChallenge> CreateAndSign(LoginData login)
    {
        var challenge = await _service.Create(login);
        _challengeKeys.Add(Key(challenge));
        Sign(login, challenge);
        return challenge;
    }

    private void Sign(LoginData login, AuthenticationChallenge challenge)
    {
        login.ChallengeId = challenge.Id;
        login.Signature = _key.SignData(AuthenticationProtocol.GetMessage(login, challenge), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    private static RedisKey Key(AuthenticationChallenge challenge) => $"auth:{AuthenticationProtocol.Epoch}:challenge:{challenge.Id}";

    [Test]
    public async Task ValidProof_IsConsumedOnRedis60_AndReplayIsRejected()
    {
        var login = Login();
        var challenge = await CreateAndSign(login);
        var lifetime = await _database.KeyTimeToLiveAsync(Key(challenge));

        var accepted = await _service.VerifyAndConsume(login);
        var replay = await _service.VerifyAndConsume(login);

        lifetime.Should().NotBeNull();
        lifetime!.Value.Should().BePositive().And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(2));
        accepted.Should().BeTrue();
        replay.Should().BeFalse();
        (await _database.KeyExistsAsync(Key(challenge))).Should().BeFalse();
    }

    [Test]
    public async Task ConcurrentConsumers_OnIndependentConnections_AuthenticateExactlyOnce()
    {
        var login = Login();
        await CreateAndSign(login);
        using var secondConnection = await ConnectionMultiplexer.ConnectAsync(Options());
        var secondService = Service(secondConnection.GetDatabase());

        var results = await Task.WhenAll(Enumerable.Range(0, 30)
            .Select(i => (i % 2 == 0 ? _service : secondService).VerifyAndConsume(login)));

        results.Count(accepted => accepted).Should().Be(1);
    }

    [Test]
    public async Task InvalidSignature_BurnsItsChallenge_AndPreservesAnotherChallenge()
    {
        var login = Login();
        var challenge = await CreateAndSign(login);
        var other = Login();
        await CreateAndSign(other);
        login.Signature[0] ^= 1;

        (await _service.VerifyAndConsume(login)).Should().BeFalse();
        Sign(login, challenge);
        (await _service.VerifyAndConsume(login)).Should().BeFalse();

        (await _service.VerifyAndConsume(other)).Should().BeTrue();
    }

    [Test]
    public async Task RedisExpiredChallenge_ReturnsNil_AndIsRejected()
    {
        var login = Login();
        var challenge = await CreateAndSign(login);
        await _database.KeyExpireAsync(Key(challenge), TimeSpan.FromMilliseconds(1));
        await Task.Delay(50);

        var accepted = await _service.VerifyAndConsume(login);

        accepted.Should().BeFalse();
        (await _database.KeyExistsAsync(Key(challenge))).Should().BeFalse();
    }

    [TearDown]
    public async Task RemoveOnlyCreatedChallenges()
    {
        if (_challengeKeys.Count != 0) await _database.KeyDeleteAsync(_challengeKeys.ToArray());
        _key?.Dispose();
    }

    [OneTimeTearDown]
    public void Disconnect() => _connection?.Dispose();
}
