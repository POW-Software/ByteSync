using ByteSync.Common.Business.Auth;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace ByteSync.ServerCommon.Tests.Security;

public class SignalRAuthorizationSecurityTests
{
    [Test]
    public async Task Reconnection_RechecksMembershipAndIgnoresLegacySubscriptions()
    {
        var client = new Client { Identity = new string('A', 64), AuthenticationEpoch = AuthenticationProtocol.Epoch,
            ClientInstanceId = new string('A', 64) + "_" + Guid.NewGuid().ToString("D") };
        var groups = new ClientsGroupIdFactory();
        client.SubscribedGroups = new() { groups.GetSessionGroupId("allowed"), groups.GetSessionGroupId("removed"), "Session_legacy" };
        var sessions = A.Fake<ICloudSessionsRepository>();
        var session = new CloudSessionData { AuthenticationEpoch = AuthenticationProtocol.Epoch };
        session.SessionMembers.Add(new SessionMemberData { ClientInstanceId = client.ClientInstanceId, Identity = client.Identity });
        A.CallTo(() => sessions.Get("allowed")).Returns(session);
        A.CallTo(() => sessions.Get("removed")).Returns(new CloudSessionData());
        var context = A.Fake<IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush>>();
        var manager = A.Fake<IGroupManager>();
        A.CallTo(() => context.Groups).Returns(manager);
        var service = new ClientsGroupsHubService(context, groups, NullLogger<ClientsGroupsHubService>.Instance, sessions, A.Fake<ILobbyRepository>());

        await service.AddClientGroup("reconnected", client);

        A.CallTo(() => manager.AddToGroupAsync("reconnected", groups.GetClientGroupId(client.ClientInstanceId), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => manager.AddToGroupAsync("reconnected", groups.GetSessionGroupId("allowed"), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => manager.AddToGroupAsync("reconnected", groups.GetSessionGroupId("removed"), A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => manager.AddToGroupAsync("reconnected", "Session_legacy", A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task LeavingSession_RemovesEveryConnectionOfTheInstance()
    {
        var client = new Client { ConnectionIds = new() { "old-connection", "new-connection", "old-connection" } };
        var context = A.Fake<IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush>>();
        var manager = A.Fake<IGroupManager>();
        A.CallTo(() => context.Groups).Returns(manager);
        var groups = new ClientsGroupIdFactory();
        var service = new ClientsGroupsHubService(context, groups, NullLogger<ClientsGroupsHubService>.Instance,
            A.Fake<ICloudSessionsRepository>(), A.Fake<ILobbyRepository>());

        await service.RemoveFromSessionGroup(client, "session");

        foreach (var id in client.ConnectionIds.Distinct())
            A.CallTo(() => manager.RemoveFromGroupAsync(id, groups.GetSessionGroupId("session"), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task LegacyIdentity_CannotAcquireAnySignalRGroup()
    {
        var client = new Client { ClientInstanceId = "legacy-instance" };
        var context = A.Fake<IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush>>();
        var service = new ClientsGroupsHubService(context, new ClientsGroupIdFactory(), NullLogger<ClientsGroupsHubService>.Instance,
            A.Fake<ICloudSessionsRepository>(), A.Fake<ILobbyRepository>());

        var act = () => service.AddClientGroup("connection", client);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        A.CallTo(context).MustNotHaveHappened();
    }
}
