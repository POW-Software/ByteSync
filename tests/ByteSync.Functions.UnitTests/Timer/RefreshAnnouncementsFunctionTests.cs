using ByteSync.Functions.Timer;
using ByteSync.Common.Business.Announcements;
using ByteSync.ServerCommon.Interfaces.Loaders;
using ByteSync.ServerCommon.Interfaces.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.Functions.UnitTests.Timer;

[TestFixture]
public class RefreshAnnouncementsFunctionTests
{
    private Mock<IAnnouncementsLoader> _loader = null!;
    private Mock<IAnnouncementRepository> _repository = null!;
    private Mock<ILogger<RefreshAnnouncementsFunction>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _loader = new Mock<IAnnouncementsLoader>();
        _repository = new Mock<IAnnouncementRepository>();
        _logger = new Mock<ILogger<RefreshAnnouncementsFunction>>();
    }

    [Test]
    public async Task RunAsync_ShouldFilterExpiredMessages()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var announcements = new List<Announcement>
        {
            new Announcement { Id = Guid.NewGuid().ToString("D"), StartDate = now.AddHours(-1), EndDate = now.AddHours(1), Message = new Dictionary<string,string>{{"en","valid"}} },
            new Announcement { Id = Guid.NewGuid().ToString("D"), StartDate = now.AddHours(-2), EndDate = now.AddHours(-1), Message = new Dictionary<string,string>{{"en","expired"}} }
        };
        _loader.Setup(l => l.Load()).ReturnsAsync(announcements);
        var function = new RefreshAnnouncementsFunction(_loader.Object, _repository.Object, _logger.Object);

        // Act
        var result = await function.RunAsync(new TimerInfo());

        // Assert
        _loader.Verify(l => l.Load(), Times.Once);
        _repository.Verify(r => r.SaveAll(It.Is<List<Announcement>>(l => l.Count == 1 && l[0].Message["en"] == "valid")), Times.Once);
        Assert.That(result, Is.EqualTo(1));
    }
}
