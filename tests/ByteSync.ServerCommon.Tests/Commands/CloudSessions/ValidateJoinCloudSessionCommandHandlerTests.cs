using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Services;
using Moq;
using NUnit.Framework;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

[TestFixture]
public class ValidateJoinCloudSessionCommandHandlerTests
{
    [Test]
    public async Task Handle_CallsService()
    {
        // Arrange
        var mockService = new Mock<ICloudSessionsService>();
        var parameters = new ValidateJoinCloudSessionParameters();
        mockService.Setup(s => s.ValidateJoinCloudSession(parameters)).Returns(Task.CompletedTask);
        var handler = new ValidateJoinCloudSessionCommandHandler(mockService.Object);
        var request = new ValidateJoinCloudSessionRequest(parameters);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        mockService.Verify(s => s.ValidateJoinCloudSession(parameters), Times.Once);
    }
} 