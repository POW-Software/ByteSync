using System.Net;
using ByteSync.Common.Business.Announcements;
using ByteSync.Functions.Http;
using ByteSync.Functions.UnitTests.TestHelpers;
using ByteSync.ServerCommon.Commands.Announcements;
using FluentAssertions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.Http;

[TestFixture]
public class AnnouncementFunctionTests
{
    [Test]
    public async Task GetAnnouncements_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        var expectedAnnouncements = new List<Announcement>
        {
            new()
            {
                Id = "announcement-1",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(7),
                Message = new Dictionary<string, string> { ["en"] = "Test announcement" }
            }
        };
        
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActiveAnnouncementsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAnnouncements);
        
        var function = new AnnouncementFunction(mediatorMock.Object);
        var mockContext = new Mock<FunctionContext>();
        var context = mockContext.Object;
        var request = new FakeHttpRequestData(context);
        
        var response = await function.GetAnnouncements(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetActiveAnnouncementsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task GetAnnouncements_ReturnsEmptyList_WhenNoAnnouncements()
    {
        var mediatorMock = new Mock<IMediator>();
        var emptyAnnouncements = new List<Announcement>();
        
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActiveAnnouncementsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyAnnouncements);
        
        var function = new AnnouncementFunction(mediatorMock.Object);
        var mockContext = new Mock<FunctionContext>();
        var context = mockContext.Object;
        var request = new FakeHttpRequestData(context);
        
        var response = await function.GetAnnouncements(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetActiveAnnouncementsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
