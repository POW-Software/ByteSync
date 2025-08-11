using ByteSync.Common.Business.Announcements;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Repositories;
using ByteSync.Services.Announcements;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Announcements;

[TestFixture]
public class AnnouncementServiceTests
{
    private Mock<IAnnouncementApiClient> _apiClient = null!;
    private Mock<IAnnouncementRepository> _repository = null!;
    private Mock<ILogger<AnnouncementService>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _apiClient = new Mock<IAnnouncementApiClient>();
        _repository = new Mock<IAnnouncementRepository>();
        _logger = new Mock<ILogger<AnnouncementService>>();
    }

    [Test]
    public async Task Start_ShouldLoadAnnouncementsInitially()
    {
        // Arrange
        var announcements = new List<Announcement> { new() { Id = "1" } };
        _apiClient.Setup(a => a.GetAnnouncements()).ReturnsAsync(announcements);

        using var service = new AnnouncementService(_apiClient.Object, _repository.Object, _logger.Object);

        // Act
        await service.Start();
        service.Dispose();

        // Assert
        _apiClient.Verify(a => a.GetAnnouncements(), Times.Once);
        _repository.Verify(r => r.Clear(), Times.Once);
        _repository.Verify(r => r.AddOrUpdate(announcements), Times.Once);
    }

    private class TestAnnouncementService : AnnouncementService
    {
        private readonly TimeSpan _delay;
        public TestAnnouncementService(IAnnouncementApiClient apiClient, IAnnouncementRepository repository, ILogger<AnnouncementService> logger, TimeSpan delay)
            : base(apiClient, repository, logger)
        {
            _delay = delay;
        }

        protected override TimeSpan RefreshDelay => _delay;
    }
    
    [Test]
    public async Task Start_ShouldRefreshAnnouncementsPeriodically()
    {
        // Arrange
        var announcements = new List<Announcement> { new() { Id = "1" } };
        _apiClient.Setup(a => a.GetAnnouncements()).ReturnsAsync(announcements);

        using var service = new TestAnnouncementService(_apiClient.Object, _repository.Object, _logger.Object, TimeSpan.FromMilliseconds(50));

        // Act
        await service.Start();
        await Task.Delay(160);
        service.Dispose();

        // Assert
        _apiClient.Verify(a => a.GetAnnouncements(), Times.AtLeast(2));
        _repository.Verify(r => r.Clear(), Times.AtLeast(2));
        _repository.Verify(r => r.AddOrUpdate(It.IsAny<IEnumerable<Announcement>>()), Times.AtLeast(2));
    }
    
}
