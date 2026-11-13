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
    private IDatabaseAsync _database = null!;

    [SetUp]
    public void SetUp()
    {
        _key = RSA.Create(2048);
        _store = new();
        _database = A.Fake<IDatabaseAsync>();
        A.CallTo(_database).Where(call => call.Method.Name == "StringSetAsync").WithReturnType<Task<bool>>()
            .ReturnsLazily(call => Task.FromResult(_store.TryAdd(call.Arguments[0]!.ToString()!, call.Arguments[1]!.ToString()!)));
        A.CallTo(() => _database.StringGetDeleteAsync(A<RedisKey>._, A<CommandFlags>._))
            .Throws(new RedisServerException("ERR unknown command 'GETDEL'"));
        A.CallTo(() => _database.ScriptEvaluateAsync(A<string>._, A<RedisKey[]>._, A<RedisValue[]>._, A<CommandFlags>._))
            .ReturnsLazily((string script, RedisKey[] keys, RedisValue[] values, CommandFlags flags) =>
                RedisResult.Create(_store.TryRemove(keys.Single().ToString(), out var value) ? (RedisValue)value : RedisValue.Null));
        var redis = A.Fake<IRedisInfrastructureService>();
        A.CallTo(() => redis.GetDatabase()).Returns(_database);
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
    [Test]
    public async Task Challenge_Consumption_UsesOneScriptWithAnExplicitKeyAndNoSeparateCommands()
    {
        var login = Login();
        var challenge = await _service.Create(login);
        Sign(login, challenge);
        Fake.ClearRecordedCalls(_database);

        var authenticated = await _service.VerifyAndConsume(login);

        authenticated.Should().BeTrue();
        var calls = Fake.GetCalls(_database).ToList();
        calls.Should().ContainSingle();
        var call = calls.Single();
        call.Method.Name.Should().Be(nameof(IDatabaseAsync.ScriptEvaluateAsync));
        call.Arguments.Get<RedisKey[]>(1).Should().Equal((RedisKey)$"auth:{AuthenticationProtocol.Epoch}:challenge:{challenge.Id}");
        call.Arguments.Get<RedisValue[]>(2).Should().BeEmpty();
    }

    [Test]
    public async Task InvalidSignature_ConsumesOnlyItsChallengeAndCannotBeRetriedWithAValidSignature()
    {
        var login = Login();
        var challenge = await _service.Create(login);
        var other = Login();
        Sign(other, await _service.Create(other));
        Sign(login, challenge);
        login.Signature[0] ^= 1;

        (await _service.VerifyAndConsume(login)).Should().BeFalse();
        Sign(login, challenge);

        (await _service.VerifyAndConsume(login)).Should().BeFalse();
        (await _service.VerifyAndConsume(other)).Should().BeTrue();
        _store.Should().BeEmpty();
    }

    [Test]
    public async Task RedisScriptFailure_DoesNotFallBackToNonAtomicConsumption()
    {
        var login = Login();
        Sign(login, await _service.Create(login));
        A.CallTo(() => _database.ScriptEvaluateAsync(A<string>._, A<RedisKey[]>._, A<RedisValue[]>._, A<CommandFlags>._))
            .Throws(new RedisServerException("ERR scripting unavailable"));
        Fake.ClearRecordedCalls(_database);

        var act = () => _service.VerifyAndConsume(login);

        await act.Should().ThrowAsync<RedisServerException>();
        Fake.GetCalls(_database).Should().ContainSingle(call => call.Method.Name == nameof(IDatabaseAsync.ScriptEvaluateAsync));
        Fake.GetCalls(_database).Should().HaveCount(1);
        _store.Should().ContainSingle();
    }

}
