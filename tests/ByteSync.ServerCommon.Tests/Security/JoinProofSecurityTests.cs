using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Security;

public class JoinProofSecurityTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task ValidationRetry_ResendsTheSameUnexpiredProof(bool firstNotificationFails)
    {
        var context = ValidationContext();
        var expiry = context.Joiner.JoinExpiresAt;
        var deliveredProofs = new List<string>();
        A.CallTo(() => context.Hub.YouJoinedSession(A<CloudSessionResult>._, A<ValidateJoinCloudSessionParameters>._))
            .ReturnsLazily((CloudSessionResult _, ValidateJoinCloudSessionParameters parameters) =>
            {
                deliveredProofs.Add(parameters.FinalizationPassword);
                return firstNotificationFails && deliveredProofs.Count == 1
                    ? Task.FromException(new IOException("Simulated lost notification")) : Task.CompletedTask;
            });
        var first = () => context.Service.ValidateJoinCloudSession(ValidationParameters(context.Joiner), context.Validator);
        if (firstNotificationFails) await first.Should().ThrowAsync<IOException>();
        else await first.Should().NotThrowAsync();
        var proof = context.Joiner.FinalizationPassword;
        var retry = ValidationParameters(context.Joiner);
        retry.FinalizationPassword = "caller-supplied-proof";

        await context.Service.ValidateJoinCloudSession(retry, context.Validator);

        proof.Should().StartWith("FP_");
        deliveredProofs.Should().Equal(proof, proof);
        retry.FinalizationPassword.Should().Be(proof);
        context.Joiner.FinalizationPassword.Should().Be(proof);
        context.Joiner.JoinExpiresAt.Should().Be(expiry);
        context.Session.PreSessionMembers.Should().ContainSingle();
    }

    [TestCase("expired")]
    [TestCase("activated")]
    [TestCase("removed")]
    [TestCase("identity")]
    [TestCase("finalized")]
    [TestCase("unknown-joiner")]
    [TestCase("validator")]
    [TestCase("epoch")]
    public async Task ValidationRetry_DoesNotReissueAnIneligibleProof(string scenario)
    {
        var context = ValidationContext();
        context.Joiner.FinalizationPassword = "existing-proof";
        var parameters = ValidationParameters(context.Joiner);
        switch (scenario)
        {
            case "expired": context.Joiner.JoinExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1); break;
            case "activated": context.Session.IsSessionActivated = true; break;
            case "removed": context.Session.IsSessionRemoved = true; break;
            case "identity": context.Validator.Identity = "another-identity"; break;
            case "finalized":
                context.Joiner.FinalizationPassword = "";
                context.Session.PreSessionMembers.Clear();
                context.Session.SessionMembers.Add(context.Joiner);
                break;
            case "unknown-joiner": parameters.JoinerClientInstanceId = "another-joiner"; break;
            case "validator": parameters.ValidatorInstanceId = "another-validator"; break;
            case "epoch": context.Validator.AuthenticationEpoch = 0; break;
        }
        var proof = context.Joiner.FinalizationPassword;
        var expiry = context.Joiner.JoinExpiresAt;

        var act = () => context.Service.ValidateJoinCloudSession(parameters, context.Validator);

        if (scenario is "validator" or "epoch") await act.Should().ThrowAsync<UnauthorizedAccessException>();
        else await act.Should().NotThrowAsync();
        context.Joiner.FinalizationPassword.Should().Be(proof);
        context.Joiner.JoinExpiresAt.Should().Be(expiry);
        A.CallTo(context.Hub).MustNotHaveHappened();
    }

    private static ValidateJoinCloudSessionParameters ValidationParameters(SessionMemberData joiner) =>
        new("session", joiner.ClientInstanceId, joiner.ValidatorInstanceId, new byte[] { 1 });

    [Test]
    public async Task ResentProof_CanStillBeFinalizedOnlyOnce()
    {
        var context = ValidationContext();
        var validation = ValidationParameters(context.Joiner);
        await context.Service.ValidateJoinCloudSession(validation, context.Validator);
        await context.Service.ValidateJoinCloudSession(ValidationParameters(context.Joiner), context.Validator);
        context.Session.SessionMembers[0].AuthCheckClientInstanceIds.Add(context.Joiner.ClientInstanceId);
        var redis = A.Fake<IRedisInfrastructureService>();
        var transaction = A.Fake<ITransaction>();
        A.CallTo(() => redis.OpenTransaction()).Returns(transaction);
        A.CallTo(() => transaction.ExecuteAsync(A<CommandFlags>._)).Returns(true);
        var handler = new FinalizeJoinCloudSessionCommandHandler(context.Repository, A.Fake<ISessionMemberMapper>(),
            A.Fake<IInvokeClientsService>(), A.Fake<IClientsGroupsService>(), redis, NullLogger<FinalizeJoinCloudSessionCommandHandler>.Instance);
        var caller = new Client { ClientInstanceId = context.Joiner.ClientInstanceId, Identity = context.Joiner.Identity,
            AuthenticationEpoch = AuthenticationProtocol.Epoch };
        var request = new FinalizeJoinCloudSessionRequest(new FinalizeJoinCloudSessionParameters
        {
            SessionId = "session", JoinerInstanceId = caller.ClientInstanceId,
            ValidatorInstanceId = context.Validator.ClientInstanceId, FinalizationPassword = validation.FinalizationPassword
        }, caller);

        var first = await handler.Handle(request, CancellationToken.None);
        var replay = await handler.Handle(request, CancellationToken.None);

        first.Status.Should().Be(FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess);
        replay.Status.Should().NotBe(FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess);
        context.Joiner.FinalizationPassword.Should().BeEmpty();
        context.Session.PreSessionMembers.Should().BeEmpty();
        context.Session.SessionMembers.Count(m => m.ClientInstanceId == caller.ClientInstanceId).Should().Be(1);
    }

    private static (CloudSessionsService Service, Client Validator, CloudSessionData Session, SessionMemberData Joiner, IHubByteSyncPush Hub, ICloudSessionsRepository Repository) ValidationContext()
    {
        var identity = new string('A', 64);
        var validator = new Client { ClientInstanceId = identity + "_" + Guid.NewGuid().ToString("D"),
            Identity = identity, AuthenticationEpoch = AuthenticationProtocol.Epoch };
        var session = new CloudSessionData { SessionId = "session", CreatorInstanceId = validator.ClientInstanceId };
        session.SessionMembers.Add(new SessionMemberData { ClientInstanceId = validator.ClientInstanceId, Identity = identity });
        var joinerIdentity = new string('B', 64);
        var joiner = new SessionMemberData { ClientInstanceId = joinerIdentity + "_" + Guid.NewGuid().ToString("D"),
            Identity = joinerIdentity, ValidatorInstanceId = validator.ClientInstanceId,
            CloudSessionData = session, JoinExpiresAt = DateTimeOffset.UtcNow.AddMinutes(2) };
        session.PreSessionMembers.Add(joiner);
        var repository = A.Fake<ICloudSessionsRepository>();
        A.CallTo(() => repository.Update("session", A<Func<CloudSessionData, bool>>._, A<ITransaction?>._, A<IRedLock?>._))
            .ReturnsLazily((string _, Func<CloudSessionData, bool> update, ITransaction? tx, IRedLock? redisLock) =>
                new UpdateEntityResult<CloudSessionData>(session, update(session)
                    ? tx == null ? UpdateEntityStatus.Saved : UpdateEntityStatus.WaitingForTransaction
                    : UpdateEntityStatus.NoOperation));
        var pushes = A.Fake<IInvokeClientsService>();
        var hub = A.Fake<IHubByteSyncPush>();
        A.CallTo(() => pushes.Client(joiner)).Returns(hub);
        var service = new CloudSessionsService(NullLogger<CloudSessionsService>.Instance, repository, A.Fake<ISessionMemberMapper>(), pushes);
        return (service, validator, session, joiner, hub, repository);
    }

    [TestCase("expired")]
    [TestCase("password")]
    [TestCase("identity")]
    [TestCase("validator")]
    [TestCase("caller")]
    [TestCase("valid")]
    public async Task Finalization_RequiresTheBoundUnexpiredProofAndConsumesItOnce(string scenario)
    {
        var identity = new string('A', 64);
        var instance = identity + "_" + Guid.NewGuid().ToString("D");
        var validator = new string('B', 64) + "_" + Guid.NewGuid().ToString("D");
        var client = new Client { Identity = identity, ClientInstanceId = instance, AuthenticationEpoch = AuthenticationProtocol.Epoch };
        var session = new CloudSessionData { SessionId = "session", AuthenticationEpoch = AuthenticationProtocol.Epoch };
        var member = new SessionMemberData { ClientInstanceId = validator, Identity = new string('B', 64), AuthCheckClientInstanceIds = new() { instance } };
        session.SessionMembers.Add(member);
        var joiner = new SessionMemberData { ClientInstanceId = instance, Identity = scenario == "identity" ? new string('C', 64) : identity,
            ValidatorInstanceId = validator, FinalizationPassword = "test-proof", JoinExpiresAt = DateTimeOffset.UtcNow.AddMinutes(scenario == "expired" ? -1 : 1), CloudSessionData = session };
        session.PreSessionMembers.Add(joiner);
        var parameters = new FinalizeJoinCloudSessionParameters { SessionId = "session", JoinerInstanceId = instance,
            ValidatorInstanceId = scenario == "validator" ? "other" : validator, FinalizationPassword = scenario == "password" ? "wrong" : "test-proof" };
        if (scenario == "caller") client.ClientInstanceId = "other-caller";
        var repository = A.Fake<ICloudSessionsRepository>();
        A.CallTo(() => repository.Update("session", A<Func<CloudSessionData, bool>>._, A<ITransaction?>._, A<IRedLock?>._))
            .ReturnsLazily((string id, Func<CloudSessionData, bool> update, ITransaction? tx, IRedLock? redisLock) =>
                new UpdateEntityResult<CloudSessionData>(session, update(session) ? UpdateEntityStatus.WaitingForTransaction : UpdateEntityStatus.NoOperation));
        var redis = A.Fake<IRedisInfrastructureService>();
        var transaction = A.Fake<ITransaction>();
        A.CallTo(() => redis.OpenTransaction()).Returns(transaction);
        A.CallTo(() => transaction.ExecuteAsync(A<CommandFlags>._)).Returns(true);
        var groups = A.Fake<IClientsGroupsService>();
        var pushes = A.Fake<IInvokeClientsService>();
        var hub = A.Fake<IHubByteSyncPush>();
        A.CallTo(() => pushes.SessionGroup("session")).Returns(hub);
        var handler = new FinalizeJoinCloudSessionCommandHandler(repository, A.Fake<ISessionMemberMapper>(), pushes, groups, redis,
            NullLogger<FinalizeJoinCloudSessionCommandHandler>.Instance);
        var request = new FinalizeJoinCloudSessionRequest(parameters, client);
        if (scenario == "caller")
        {
            var act = () => handler.Handle(request, CancellationToken.None);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
        else
        {
            var result = await handler.Handle(request, CancellationToken.None);
            if (scenario == "valid")
            {
                result.Status.Should().Be(FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess);
                joiner.FinalizationPassword.Should().BeEmpty();
                var replay = await handler.Handle(request, CancellationToken.None);
                replay.Status.Should().NotBe(FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess);
                A.CallTo(() => hub.MemberJoinedSession(A<ByteSync.Common.Business.Sessions.Cloud.SessionMemberInfoDTO>._)).MustHaveHappenedOnceExactly();
                return;
            }
            result.Status.Should().NotBe(FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess);
        }
        session.SessionMembers.Should().NotContain(joiner);
        A.CallTo(groups).MustNotHaveHappened();
        A.CallTo(() => hub.MemberJoinedSession(A<ByteSync.Common.Business.Sessions.Cloud.SessionMemberInfoDTO>._)).MustNotHaveHappened();
    }
}
