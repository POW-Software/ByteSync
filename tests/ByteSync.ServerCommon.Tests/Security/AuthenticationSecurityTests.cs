using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Security;

public class AuthenticationSecurityTests
{
    private RSA _key = null!;
    private AuthenticationChallengeService _service = null!;
    private ConcurrentDictionary<string, string> _store = null!;

    [SetUp]
    public void SetUp()
    {
        _key = RSA.Create(2048);
        _store = new();
        var database = A.Fake<IDatabaseAsync>();
        A.CallTo(database).Where(call => call.Method.Name == "StringSetAsync").WithReturnType<Task<bool>>()
            .ReturnsLazily(call => Task.FromResult(_store.TryAdd(call.Arguments[0]!.ToString()!, call.Arguments[1]!.ToString()!)));
        A.CallTo(() => database.StringGetDeleteAsync(A<RedisKey>._, A<CommandFlags>._))
            .ReturnsLazily((RedisKey key, CommandFlags flags) => _store.TryRemove(key.ToString(), out var value) ? (RedisValue)value : RedisValue.Null);
        var redis = A.Fake<IRedisInfrastructureService>();
        A.CallTo(() => redis.GetDatabase()).Returns(database);
        _service = new AuthenticationChallengeService(redis);
    }

    [TearDown]
    public void TearDown() => _key.Dispose();

    private LoginData Login()
    {
        var identity = AuthenticationProtocol.GetIdentity(_key.ExportRSAPublicKey());
        return new LoginData { Identity = identity, ClientId = "legacy-display-id", ClientInstanceId = identity + "_" + Guid.NewGuid().ToString("D"),
            PublicKey = _key.ExportRSAPublicKey(), AuthenticationEpoch = AuthenticationProtocol.Epoch,
            ProtocolVersion = ProtocolVersion.CURRENT, Version = "2026.1", OsPlatform = OSPlatforms.Windows };
    }

    private void Sign(LoginData login, AuthenticationChallenge challenge)
    {
        login.ChallengeId = challenge.Id;
        login.Signature = _key.SignData(AuthenticationProtocol.GetMessage(login, challenge), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    [Test]
    public async Task Login_WithDurableKey_AllowsDistinctInstancesAndRejectsReplay()
    {
        var first = Login();
        var second = Login();
        Sign(first, await _service.Create(first));
        Sign(second, await _service.Create(second));

        (await _service.VerifyAndConsume(first)).Should().BeTrue();
        (await _service.VerifyAndConsume(second)).Should().BeTrue();
        (await _service.VerifyAndConsume(first)).Should().BeFalse();
        first.Identity.Should().Be(second.Identity);
        first.Identity.Should().HaveLength(64);
    }

    [TestCase("signature")]
    [TestCase("key")]
    [TestCase("identity")]
    [TestCase("instance")]
    [TestCase("version")]
    [TestCase("epoch")]
    [TestCase("protocol")]
    public async Task Login_WithAlteredProof_IsRejected(string field)
    {
        var login = Login();
        Sign(login, await _service.Create(login));
        using var other = RSA.Create(2048);
        switch (field)
        {
            case "signature": login.Signature[0] ^= 1; break;
            case "key": login.PublicKey = other.ExportRSAPublicKey(); break;
            case "identity": login.Identity = new string('A', 64); break;
            case "instance": login.ClientInstanceId = login.Identity + "_" + Guid.NewGuid().ToString("D"); break;
            case "version": login.Version = "other"; break;
            case "epoch": login.AuthenticationEpoch = 0; break;
            case "protocol": login.ProtocolVersion = 2; break;
        }

        (await _service.VerifyAndConsume(login)).Should().BeFalse();
    }

    [Test]
    public async Task Challenge_ConsumedConcurrently_AuthenticatesExactlyOnce()
    {
        var login = Login();
        Sign(login, await _service.Create(login));

        var results = await Task.WhenAll(Enumerable.Range(0, 30).Select(_ => Task.Run(() => _service.VerifyAndConsume(login))));

        results.Count(ok => ok).Should().Be(1);
    }

    [Test]
    public async Task Challenge_WhenExpired_IsRejectedEvenIfStillStored()
    {
        var login = Login();
        var challenge = await _service.Create(login);
        var key = _store.Keys.Single();
        var record = JsonNode.Parse(_store[key])!;
        challenge.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        record["Challenge"]!["ExpiresAt"] = challenge.ExpiresAt;
        _store[key] = record.ToJsonString();
        Sign(login, challenge);

        (await _service.VerifyAndConsume(login)).Should().BeFalse();
    }

    [Test]
    public async Task Challenge_CannotClaimAnInstanceFromAnotherIdentity()
    {
        var login = Login();
        login.ClientInstanceId = "old-client-id_" + Guid.NewGuid();

        var act = () => _service.Create(login);

        await act.Should().ThrowAsync<CryptographicException>();
        _store.Should().BeEmpty();
    }

    [Test]
    public async Task MissingOrMalformedChallenge_IsRejected()
    {
        var login = Login();
        login.ChallengeId = "invalid";
        (await _service.VerifyAndConsume(login)).Should().BeFalse();
        login.ChallengeId = Guid.NewGuid().ToString("N");
        (await _service.VerifyAndConsume(login)).Should().BeFalse();
    }
}
