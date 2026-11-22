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
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using ByteSync.ServerCommon.Tests.Helpers;
using ByteSync.TestsCommon.TestHelpers;
using Microsoft.Extensions.Options;

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
    public async Task ReadableAlias_CannotMergeDifferentKeysOrClaimALegacyRecord()
    {
        using var first = new AuthenticationTestIdentity();
        using var second = new AuthenticationTestIdentity(first.ClientId);
        var legacy = new Client(first.ClientId, "legacy-instance", "old", OSPlatforms.Windows, "127.0.0.1");
        var records = new Dictionary<string, Client> { [legacy.ClientInstanceId] = legacy };
        var clients = A.Fake<IClientsRepository>();
        A.CallTo(() => clients.AddOrUpdate(A<string>._, A<Func<Client?, Client?>>._))
            .ReturnsLazily((string instance, Func<Client?, Client?> update) =>
            {
                var updated = update(records.GetValueOrDefault(instance));
                if (updated != null) records[instance] = updated;
                return UpdateResultBuilder.BuildAddOrUpdateResult(updated, false);
            });
        var versions = A.Fake<IClientSoftwareVersionService>();
        A.CallTo(() => versions.IsClientVersionAllowed(A<LoginData>._)).Returns(true);
        var tokens = new TokensFactory(Options.Create(new AppSettings
        {
            Secret = "offline-alias-collision-signing-key-at-least-32-bytes", JwtDurationInSeconds = 300
        }));
        var handler = new AuthenticateCommandHandler(tokens, new ByteSync.ServerCommon.Factories.ByteSyncEndpointFactory(), clients, versions, _service);

        foreach (var identity in new[] { first, second })
        {
            var login = identity.CreateLogin();
            identity.Sign(login, await _service.Create(login));
            var response = await handler.Handle(new AuthenticateRequest(login, "127.0.0.1"), CancellationToken.None);
            response.IsSuccess.Should().BeTrue();
            response.EndPoint!.ClientId.Should().Be(first.ClientId);
            response.EndPoint.ClientInstanceId.Should().Be(identity.ClientInstanceId);
        }

        records.Should().HaveCount(3);
        records[first.ClientInstanceId].Identity.Should().Be(first.Identity);
        records[second.ClientInstanceId].Identity.Should().Be(second.Identity).And.NotBe(first.Identity);
        records[legacy.ClientInstanceId].Should().BeSameAs(legacy);
        legacy.Identity.Should().BeNull();
        legacy.AuthenticationEpoch.Should().Be(0);
        var takeover = second.CreateLogin();
        takeover.ClientInstanceId = first.ClientInstanceId;
        var act = () => _service.Create(takeover);
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Test]
    public async Task ClientAliasChangedAfterChallenge_IsRejectedEvenWithANewValidSignature()
    {
        using var identity = new AuthenticationTestIdentity();
        var login = identity.CreateLogin();
        var challenge = await _service.Create(login);
        login.ClientId = "KLMN-OPQ-RST";
        identity.Sign(login, challenge);

        var accepted = await _service.VerifyAndConsume(login);

        accepted.Should().BeFalse();
        _store.Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task MissingClientAlias_CannotCreateAChallenge(string? clientId)
    {
        var login = Login();
        login.ClientId = clientId!;

        var act = () => _service.Create(login);

        await act.Should().ThrowAsync<CryptographicException>();
        _store.Should().BeEmpty();
    }

    [Test]
    public async Task IntegrationIdentity_UsesSignedChallengesAndProducesRefreshableCurrentEpochTokens()
    {
        using var identity = new AuthenticationTestIdentity();
        var login = identity.CreateLogin();
        var keyInfo = identity.CreatePublicKeyInfo();
        AuthenticationProtocol.GetIdentity(keyInfo.PublicKey).Should().Be(identity.Identity);
        keyInfo.ProtocolVersion.Should().Be(ProtocolVersion.CURRENT);
        keyInfo.ClientId.Should().Be(identity.ClientId).And.NotBe(identity.Identity);
        identity.Sign(login, await _service.Create(login));
        Client? saved = null;
        var clients = A.Fake<IClientsRepository>();
        A.CallTo(() => clients.AddOrUpdate(identity.ClientInstanceId, A<Func<Client?, Client?>>._))
            .ReturnsLazily((string _, Func<Client?, Client?> update) =>
            {
                var updated = update(saved);
                if (updated != null) saved = updated;
                return UpdateResultBuilder.BuildAddOrUpdateResult(updated, false);
            });
        var versions = A.Fake<IClientSoftwareVersionService>();
        A.CallTo(() => versions.IsClientVersionAllowed(login)).Returns(true);
        var tokens = new TokensFactory(Options.Create(new AppSettings
        {
            Secret = "offline-fixture-validation-signing-key-at-least-32-bytes", JwtDurationInSeconds = 300
        }));
        var handler = new AuthenticateCommandHandler(tokens, new ByteSync.ServerCommon.Factories.ByteSyncEndpointFactory(), clients, versions, _service);

        var response = await handler.Handle(new AuthenticateRequest(login, "127.0.0.1"), CancellationToken.None);
        var originalRefresh = response.AuthenticationTokens!.RefreshToken;
        var refreshed = await new RefreshTokensCommandHandler(tokens, clients).Handle(new RefreshTokensRequest(
            new RefreshTokensData { ClientInstanceId = identity.ClientInstanceId, Token = originalRefresh }, "127.0.0.1"), CancellationToken.None);
        var replay = await handler.Handle(new AuthenticateRequest(login, "127.0.0.1"), CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        saved!.ClientId.Should().Be(identity.ClientId);
        response.EndPoint!.ClientId.Should().Be(identity.ClientId);
        saved!.Identity.Should().Be(identity.Identity);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(response.AuthenticationTokens.JwtToken);
        jwt.Claims.Single(c => c.Type == AuthConstants.CLAIM_CLIENT_ID).Value.Should().Be(identity.ClientId);
        jwt.Claims.Single(c => c.Type == AuthConstants.CLAIM_IDENTITY).Value.Should().Be(identity.Identity);
        saved.AuthenticationEpoch.Should().Be(AuthenticationProtocol.Epoch);
        refreshed.RefreshTokensStatus.Should().Be(RefreshTokensStatus.RefreshTokenOk);
        refreshed.AuthenticationTokens!.RefreshToken.Should().NotBe(originalRefresh);
        saved.RefreshToken!.AuthenticationEpoch.Should().Be(AuthenticationProtocol.Epoch);
        var reusedRefresh = await new RefreshTokensCommandHandler(tokens, clients).Handle(new RefreshTokensRequest(
            new RefreshTokensData { ClientInstanceId = identity.ClientInstanceId, Token = originalRefresh }, "127.0.0.1"), CancellationToken.None);
        reusedRefresh.IsSuccess.Should().BeFalse();
        replay.InitialConnectionStatus.Should().Be(InitialConnectionStatus.InvalidProof);
        replay.AuthenticationTokens.Should().BeNull();
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
    [TestCase("client-id")]
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
            case "client-id": login.ClientId = "OTHER-CLI-ENT"; break;
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
