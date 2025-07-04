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
public class AskPasswordExchangeKeyCommandHandlerTests
{
    [Test]
    public async Task Handle_ReturnsJoinSessionResult()
    {
        var mockService = new Mock<ICloudSessionsService>();
        var expected = new JoinSessionResult();
        var client = new Client();
        var parameters = new AskCloudSessionPasswordExchangeKeyParameters();
        mockService.Setup(s => s.AskCloudSessionPasswordExchangeKey(client, parameters)).ReturnsAsync(expected);
        var handler = new AskPasswordExchangeKeyCommandHandler(mockService.Object);
        var request = new AskPasswordExchangeKeyRequest(client, parameters);
        var result = await handler.Handle(request, CancellationToken.None);
        Assert.That(result, Is.EqualTo(expected));
    }
} 