using ByteSync.Common.Business.EndPoints;
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
public class RequestTrustPublicKeyCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenRecipientExists_InvokesClientRequest()
    {
        // Arrange
        var sessionId = "session-1";
        var recipientInstanceId = "recipient-instance";
        var senderInstanceId = "sender-instance";

        var repo = new Mock<ICloudSessionsRepository>();
        repo.Setup(r => r.GetSessionMember(sessionId, recipientInstanceId))
            .ReturnsAsync(new SessionMemberData { ClientInstanceId = recipientInstanceId, ClientId = "r", CloudSessionData = new CloudSessionData { SessionId = sessionId } });

        var hub = new Mock<IHubByteSyncPush>();
        var invoke = new Mock<IInvokeClientsService>();
        invoke.Setup(s => s.Client(It.IsAny<SessionMemberData>())).Returns(hub.Object);

        var logger = new Mock<ILogger<SetAuthCheckedCommandHandler>>();
        var handler = new RequestTrustPublicKeyCommandHandler(repo.Object, invoke.Object, logger.Object);

        var parameters = new RequestTrustProcessParameters(sessionId, new PublicKeyCheckData(), recipientInstanceId);
        var client = new Client { ClientInstanceId = senderInstanceId, ClientId = "s" };
        var request = new RequestTrustPublicKeyRequest(parameters, client);

        // Act
        await handler.Handle(request, default);

        // Assert
        invoke.Verify(s => s.Client(It.Is<SessionMemberData>(m => m.ClientInstanceId == recipientInstanceId)), Times.Once);
        hub.Verify(h => h.RequestTrustPublicKey(parameters), Times.Once);
    }

    [Test]
    public async Task Handle_WhenRecipientNotFound_DoesNothing()
    {
        // Arrange
        var sessionId = "session-1";
        var recipientInstanceId = "recipient-instance";

        var repo = new Mock<ICloudSessionsRepository>();
        repo.Setup(r => r.GetSessionMember(sessionId, recipientInstanceId))
            .ReturnsAsync((SessionMemberData?)null);

        var hub = new Mock<IHubByteSyncPush>();
        var invoke = new Mock<IInvokeClientsService>();
        invoke.Setup(s => s.Client(It.IsAny<SessionMemberData>())).Returns(hub.Object);

        var logger = new Mock<ILogger<SetAuthCheckedCommandHandler>>();
        var handler = new RequestTrustPublicKeyCommandHandler(repo.Object, invoke.Object, logger.Object);

        var parameters = new RequestTrustProcessParameters(sessionId, new PublicKeyCheckData(), recipientInstanceId);
        var client = new Client { ClientInstanceId = "sender", ClientId = "s" };
        var request = new RequestTrustPublicKeyRequest(parameters, client);

        // Act
        await handler.Handle(request, default);

        // Assert
        invoke.Verify(s => s.Client(It.IsAny<SessionMemberData>()), Times.Never);
        hub.Verify(h => h.RequestTrustPublicKey(It.IsAny<RequestTrustProcessParameters>()), Times.Never);
    }
}