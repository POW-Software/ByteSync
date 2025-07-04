using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Services;
using Moq;
using NUnit.Framework;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

[TestFixture]
public class GetMembersCommandHandlerTests
{
    [Test]
    public async Task Handle_ReturnsMembers()
    {
        var mockService = new Mock<ICloudSessionsService>();
        var expected = new List<SessionMemberInfoDTO> { new SessionMemberInfoDTO(), new SessionMemberInfoDTO() };
        mockService.Setup(s => s.GetSessionMembersInfosAsync("session1")).ReturnsAsync(expected);
        var handler = new GetMembersCommandHandler(mockService.Object);
        var request = new GetMembersRequest("session1");
        var result = await handler.Handle(request, CancellationToken.None);
        Assert.That(result, Is.EqualTo(expected));
    }
} 