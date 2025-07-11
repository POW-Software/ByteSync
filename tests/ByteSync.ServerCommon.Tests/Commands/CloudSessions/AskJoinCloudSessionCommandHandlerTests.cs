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
public class AskJoinCloudSessionCommandHandlerTests
{
    [Test]
    public async Task Handle_ReturnsJoinSessionResult()
    {
        // Arrange
        var mockService = new Mock<ICloudSessionsService>();
        var expected = new JoinSessionResult();
        var client = new Client();
        var parameters = new AskJoinCloudSessionParameters();
        mockService.Setup(s => s.AskJoinCloudSession(client, parameters)).ReturnsAsync(expected);
        var handler = new AskJoinCloudSessionCommandHandler(mockService.Object);
        var request = new AskJoinCloudSessionRequest(client, parameters);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
} 