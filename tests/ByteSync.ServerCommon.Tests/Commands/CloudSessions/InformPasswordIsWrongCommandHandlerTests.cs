using System.Threading;
using System.Threading.Tasks;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Services;
using Moq;
using NUnit.Framework;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

[TestFixture]
public class InformPasswordIsWrongCommandHandlerTests
{
    [Test]
    public async Task Handle_CallsService()
    {
        // Arrange
        var mockService = new Mock<ICloudSessionsService>();
        var client = new Client();
        var sessionId = "session1";
        var clientInstanceId = "instance1";
        mockService.Setup(s => s.InformPasswordIsWrong(client, sessionId, clientInstanceId)).Returns(Task.CompletedTask);
        var handler = new InformPasswordIsWrongCommandHandler(mockService.Object);
        var request = new InformPasswordIsWrongRequest(client, sessionId, clientInstanceId);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        mockService.Verify(s => s.InformPasswordIsWrong(client, sessionId, clientInstanceId), Times.Once);
    }
} 