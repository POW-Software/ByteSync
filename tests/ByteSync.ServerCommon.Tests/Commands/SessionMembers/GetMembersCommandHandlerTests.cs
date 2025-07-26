using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Commands.SessionMembers;
using ByteSync.ServerCommon.Interfaces.Services;
using Moq;

namespace ByteSync.ServerCommon.Tests.Commands.SessionMembers;

[TestFixture]
public class GetMembersCommandHandlerTests
{
    [Test]
    public async Task Handle_ReturnsMembers()
    {
        // Arrange
        var mockService = new Mock<ICloudSessionsService>();
        var expected = new List<SessionMemberInfoDTO> { new SessionMemberInfoDTO(), new SessionMemberInfoDTO() };
        mockService.Setup(s => s.GetSessionMembersInfosAsync("session1")).ReturnsAsync(expected);
        var handler = new GetMembersCommandHandler(mockService.Object);
        var request = new GetMembersRequest("session1");

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
} 