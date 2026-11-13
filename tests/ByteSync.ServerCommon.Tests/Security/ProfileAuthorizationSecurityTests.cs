using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Business.Profiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ByteSync.ServerCommon.Tests.Security;

public class ProfileAuthorizationSecurityTests
{
    private static Client Client() => new() { Identity = new string('A', 64), ClientId = new string('A', 64),
        AuthenticationEpoch = AuthenticationProtocol.Epoch, ClientInstanceId = new string('A', 64) + "_" + Guid.NewGuid().ToString("D") };

    [TestCase("owned")]
    [TestCase("other-key")]
    [TestCase("legacy-profile")]
    public async Task ProfileSecretsAndDeletion_RequireCurrentEpochAndFingerprintOwnership(string scenario)
    {
        var client = Client();
        var profiles = A.Fake<ICloudSessionProfileRepository>();
        var profile = new CloudSessionProfileEntity { AuthenticationEpoch = AuthenticationProtocol.Epoch,
            CloudSessionProfileId = "profile", ProfileDetailsPassword = "local-test-only-value" };
        profile.Slots.Add(new CloudSessionProfileSlot { ClientId = client.Identity!, ProfileClientId = "slot" });
        if (scenario == "other-key") profile.Slots[0].ClientId = new string('B', 64);
        if (scenario == "legacy-profile") profile.AuthenticationEpoch = 0;
        A.CallTo(() => profiles.Get("profile")).Returns(profile);
        var service = new CloudSessionProfileService(profiles, A.Fake<ICloudSessionsRepository>(), NullLogger<CloudSessionProfileService>.Instance);

        var password = await service.GetProfileDetailsPassword(new GetProfileDetailsPasswordParameters { CloudSessionProfileId = "profile", ProfileClientId = "slot" }, client);
        var delete = () => service.DeleteCloudSessionProfile(new DeleteCloudSessionProfileParameters { CloudSessionProfileId = "profile", ProfileClientId = "slot" }, client);

        if (scenario == "owned")
        {
            password.Should().Be(profile.ProfileDetailsPassword);
            (await delete()).Should().BeTrue();
            A.CallTo(() => profiles.Delete("profile")).MustHaveHappenedOnceExactly();
        }
        else
        {
            password.Should().BeNull();
            await delete.Should().ThrowAsync<UnauthorizedAccessException>();
            A.CallTo(() => profiles.Delete(A<string>._)).MustNotHaveHappened();
        }
    }

    [Test]
    public async Task TwoInstancesOfOneIdentity_ReceiveDistinctProfileSlots()
    {
        var first = Client();
        var second = Client();
        var session = new CloudSessionData { SessionId = "session", AuthenticationEpoch = AuthenticationProtocol.Epoch };
        foreach (var client in new[] { first, second }) session.SessionMembers.Add(new SessionMemberData { ClientInstanceId = client.ClientInstanceId,
            ClientId = client.ClientId, Identity = client.Identity, CloudSessionData = session });
        var profiles = A.Fake<ICloudSessionProfileRepository>();
        var sessions = A.Fake<ICloudSessionsRepository>();
        A.CallTo(() => sessions.GetSessionMember("session", first.ClientInstanceId)).Returns(session.SessionMembers[0]);
        var service = new CloudSessionProfileService(profiles, sessions, NullLogger<CloudSessionProfileService>.Instance);
        CloudSessionProfileEntity? saved = null;
        A.CallTo(() => profiles.Save(A<string>._, A<CloudSessionProfileEntity>._, null, null))
            .Invokes((string id, CloudSessionProfileEntity value, StackExchange.Redis.ITransaction? transaction, RedLockNet.IRedLock? redisLock) => saved = value);

        await service.CreateCloudSessionProfile("session", first);
        A.CallTo(() => profiles.Get(A<string>._)).ReturnsLazily(() => saved);
        A.CallTo(() => sessions.GetSessionMember("session", second.ClientInstanceId)).Returns(session.SessionMembers[1]);
        var details = await service.GetCloudSessionProfileData(new GetCloudSessionProfileDataParameters
            { SessionId = "session", CloudSessionProfileId = saved!.CloudSessionProfileId }, second);

        details.Should().NotBeNull();
        details!.RequesterProfileClientId.Should().Be(saved!.Slots[1].ProfileClientId);
        saved.Should().NotBeNull();
        saved!.Slots.Should().HaveCount(2);
        saved.Slots.Select(s => s.ProfileClientId).Should().OnlyHaveUniqueItems();
        saved.Slots.Select(s => s.OriginalClientInstanceId).Should().BeEquivalentTo(first.ClientInstanceId, second.ClientInstanceId);
    }
}
