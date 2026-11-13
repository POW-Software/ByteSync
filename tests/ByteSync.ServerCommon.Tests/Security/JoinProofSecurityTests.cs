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
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Security;

public class JoinProofSecurityTests
{
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
