using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Services;
using Moq;
using NUnit.Framework;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

[TestFixture]
public class GetMembersInstanceIdsCommandHandlerTests
{
    [Test]
    public async Task Handle_ReturnsInstanceIds()
    {
        var mockService = new Mock<ICloudSessionsService>();
        var expected = new List<string> { "id1", "id2" };
        mockService.Setup(s => s.GetMembersInstanceIds("session1")).ReturnsAsync(expected);
        var handler = new GetMembersInstanceIdsCommandHandler(mockService.Object);
        var request = new GetMembersInstanceIdsRequest("session1");
        var result = await handler.Handle(request, CancellationToken.None);
        Assert.That(result, Is.EqualTo(expected));
    }
} 