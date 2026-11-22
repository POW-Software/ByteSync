using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Trusts;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using ByteSync.TestsCommon.TestHelpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ByteSync.ServerCommon.Tests.Commands.Trusts;

public class InformPublicKeyValidationIsFinishedCommandHandlerTests
{
    [TestCase("valid", true)]
    [TestCase("alias-collision", true)]
    [TestCase("pending-sender", true)]
    [TestCase("pending-recipient", true)]
    [TestCase("outsider", false)]
    [TestCase("wrong-identity", false)]
    [TestCase("wrong-instance", false)]
    [TestCase("expired-sender", false)]
    [TestCase("expired-recipient", false)]
    [TestCase("wrong-recipient-key", false)]
    [TestCase("malformed-key", false)]
    [TestCase("missing-key", false)]
    [TestCase("missing-exchange", false)]
    [TestCase("oversized-exchange", false)]
    [TestCase("missing-recipient", false)]
    [TestCase("missing-recipient-identity", false)]
    [TestCase("activated", false)]
    [TestCase("removed", false)]
    [TestCase("legacy-epoch", false)]
    [TestCase("legacy-protocol", false)]
    public async Task Confirmation_UsesAuthenticatedSenderAndEligibleRecipient(string scenario, bool allowed)
    {
        using var sender = new AuthenticationTestIdentity();
        using var recipient = new AuthenticationTestIdentity();
        var client = new Client { ClientId = scenario == "alias-collision" ? recipient.ClientId : "KLMN-OPQ-RST",
            ClientInstanceId = sender.ClientInstanceId, Identity = sender.Identity, AuthenticationEpoch = AuthenticationProtocol.Epoch };
        var session = new CloudSessionData { SessionId = "session", AuthenticationEpoch = AuthenticationProtocol.Epoch,
            ProtocolVersion = ProtocolVersion.CURRENT };
        var senderMember = new SessionMemberData { ClientId = client.ClientId, ClientInstanceId = sender.ClientInstanceId,
            Identity = sender.Identity, JoinExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1) };
        var recipientMember = new SessionMemberData { ClientId = recipient.ClientId, ClientInstanceId = recipient.ClientInstanceId,
            Identity = recipient.Identity, JoinExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1) };
        session.SessionMembers.Add(senderMember);
        session.SessionMembers.Add(recipientMember);
        var parameters = new PublicKeyValidationParameters { SessionId = "session", IssuerClientId = "forged-alias",
            IssuerIdentity = recipient.Identity, IssuerClientInstanceId = recipient.ClientInstanceId,
            OtherPartyClientInstanceId = recipient.ClientInstanceId, OtherPartyPublicKey = recipient.CreatePublicKeyInfo().PublicKey,
            ExchangeId = "exchange-salt", IsValidated = true };
        switch (scenario)
        {
            case "outsider": session.SessionMembers.Remove(senderMember); break;
            case "wrong-identity": senderMember.Identity = recipient.Identity; break;
            case "wrong-instance": client.ClientInstanceId = recipient.ClientInstanceId; break;
            case "pending-sender": case "expired-sender":
                session.SessionMembers.Remove(senderMember); session.PreSessionMembers.Add(senderMember);
                if (scenario == "expired-sender") senderMember.JoinExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
                break;
            case "pending-recipient": case "expired-recipient":
                session.SessionMembers.Remove(recipientMember); session.PreSessionMembers.Add(recipientMember);
                if (scenario == "expired-recipient") recipientMember.JoinExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
                break;
            case "wrong-recipient-key": parameters.OtherPartyPublicKey = sender.CreatePublicKeyInfo().PublicKey; break;
            case "malformed-key": parameters.OtherPartyPublicKey = [1, 2, 3]; break;
            case "missing-key": parameters.OtherPartyPublicKey = null!; break;
            case "missing-exchange": parameters.ExchangeId = ""; break;
            case "oversized-exchange": parameters.ExchangeId = new string('A', 129); break;
            case "missing-recipient": session.SessionMembers.Remove(recipientMember); break;
            case "missing-recipient-identity": recipientMember.Identity = null; break;
            case "activated": session.IsSessionActivated = true; break;
            case "removed": session.IsSessionRemoved = true; break;
            case "legacy-epoch": session.AuthenticationEpoch = 0; break;
            case "legacy-protocol": session.ProtocolVersion = 2; break;
        }
        var repo = A.Fake<ICloudSessionsRepository>();
        A.CallTo(() => repo.Get("session")).Returns(session);
        var hub = A.Fake<IHubByteSyncPush>();
        var invoke = A.Fake<IInvokeClientsService>();
        A.CallTo(() => invoke.Client(recipient.ClientInstanceId)).Returns(hub);
        var handler = new InformPublicKeyValidationIsFinishedCommandHandler(repo, invoke,
            NullLogger<InformPublicKeyValidationIsFinishedCommandHandler>.Instance);
        var act = () => handler.Handle(new InformPublicKeyValidationIsFinishedRequest(parameters, client), default);

        if (allowed)
        {
            await act();
            parameters.IssuerClientId.Should().Be(client.ClientId);
            parameters.IssuerClientInstanceId.Should().Be(sender.ClientInstanceId);
            parameters.IssuerIdentity.Should().Be(sender.Identity);
            parameters.ExchangeId.Should().Be("exchange-salt");
            A.CallTo(() => hub.InformPublicKeyValidationIsFinished(parameters)).MustHaveHappenedOnceExactly();
        }
        else
        {
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            A.CallTo(() => hub.InformPublicKeyValidationIsFinished(A<PublicKeyValidationParameters>._)).MustNotHaveHappened();
        }
    }

    [Test]
    public async Task MissingSession_DoesNotSendConfirmation()
    {
        var repo = A.Fake<ICloudSessionsRepository>();
        A.CallTo(() => repo.Get("missing")).Returns((CloudSessionData?)null);
        var invoke = A.Fake<IInvokeClientsService>();
        var handler = new InformPublicKeyValidationIsFinishedCommandHandler(repo, invoke,
            NullLogger<InformPublicKeyValidationIsFinishedCommandHandler>.Instance);

        await handler.Handle(new InformPublicKeyValidationIsFinishedRequest(new PublicKeyValidationParameters
            { SessionId = "missing" }, new Client()), default);

        A.CallTo(invoke).MustNotHaveHappened();
    }
}
