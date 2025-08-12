using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Services;
using Moq;
using NUnit.Framework;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

[TestFixture]
public class GiveCloudSessionPasswordExchangeKeyCommandHandlerTests
{
    [Test]
    public async Task Handle_CallsService()
    {
        // Arrange
        var mockService = new Mock<ICloudSessionsService>();
        var client = new Client();
        var parameters = new GiveCloudSessionPasswordExchangeKeyParameters();
        mockService.Setup(s => s.GiveCloudSessionPasswordExchangeKey(client, parameters)).Returns(Task.CompletedTask);
        var handler = new GiveCloudSessionPasswordExchangeKeyCommandHandler(mockService.Object);
        var request = new GiveCloudSessionPasswordExchangeKeyRequest(client, parameters);

        // Act
        await handler.Handle(request, CancellationToken.None);

        // Assert
        mockService.Verify(s => s.GiveCloudSessionPasswordExchangeKey(client, parameters), Times.Once);
    }
} 