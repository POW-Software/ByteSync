using System.Threading;
using System.Threading.Tasks;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Services;
using Moq;
using NUnit.Framework;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

[TestFixture]
public class ResetSessionCommandHandlerTests
{
    [Test]
    public async Task Handle_CallsService()
    {
        // Arrange
        var mockService = new Mock<ICloudSessionsService>();
        var client = new Client();
        var sessionId = "session1";
        mockService.Setup(s => s.ResetSession(sessionId, client)).ReturnsAsync(true);
        var handler = new ResetSessionCommandHandler(mockService.Object);
        var request = new ResetSessionRequest(sessionId, client);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        mockService.Verify(s => s.ResetSession(sessionId, client), Times.Once);
    }
} 