using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Trusts;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.ServerCommon.Tests.Commands.Trusts;

[TestFixture]
public class InformPublicKeyValidationIsFinishedCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenSessionExists_InvokesClientInform()
    {
        // Arrange
        var sessionId = "session-1";
        var otherPartyInstanceId = "other-instance";

        var repo = new Mock<ICloudSessionsRepository>();
        repo.Setup(r => r.Get(sessionId)).ReturnsAsync(new CloudSessionData { SessionId = sessionId });

        var hub = new Mock<IHubByteSyncPush>();
        var invoke = new Mock<IInvokeClientsService>();
        invoke.Setup(s => s.Client(otherPartyInstanceId)).Returns(hub.Object);

        var logger = new Mock<ILogger<InformPublicKeyValidationIsFinishedCommandHandler>>();
        var handler = new InformPublicKeyValidationIsFinishedCommandHandler(repo.Object, invoke.Object, logger.Object);

        var parameters = new PublicKeyValidationParameters { SessionId = sessionId, OtherPartyClientInstanceId = otherPartyInstanceId };
        var client = new Client { ClientInstanceId = "sender", ClientId = "s" };
        var request = new InformPublicKeyValidationIsFinishedRequest(parameters, client);

        // Act
        await handler.Handle(request, default);

        // Assert
        invoke.Verify(s => s.Client(otherPartyInstanceId), Times.Once);
        hub.Verify(h => h.InformPublicKeyValidationIsFinished(parameters), Times.Once);
    }

    [Test]
    public async Task Handle_WhenSessionMissing_DoesNothing()
    {
        // Arrange
        var sessionId = "session-1";
        var otherPartyInstanceId = "other-instance";

        var repo = new Mock<ICloudSessionsRepository>();
        repo.Setup(r => r.Get(sessionId)).ReturnsAsync((CloudSessionData?)null);

        var hub = new Mock<IHubByteSyncPush>();
        var invoke = new Mock<IInvokeClientsService>();
        invoke.Setup(s => s.Client(It.IsAny<string>())).Returns(hub.Object);

        var logger = new Mock<ILogger<InformPublicKeyValidationIsFinishedCommandHandler>>();
        var handler = new InformPublicKeyValidationIsFinishedCommandHandler(repo.Object, invoke.Object, logger.Object);

        var parameters = new PublicKeyValidationParameters { SessionId = sessionId, OtherPartyClientInstanceId = otherPartyInstanceId };
        var client = new Client { ClientInstanceId = "sender", ClientId = "s" };
        var request = new InformPublicKeyValidationIsFinishedRequest(parameters, client);

        // Act
        await handler.Handle(request, default);

        // Assert
        invoke.Verify(s => s.Client(It.IsAny<string>()), Times.Never);
        hub.Verify(h => h.InformPublicKeyValidationIsFinished(It.IsAny<PublicKeyValidationParameters>()), Times.Never);
    }
}