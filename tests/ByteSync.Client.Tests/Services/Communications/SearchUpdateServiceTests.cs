using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;
using ByteSync.Services.Updates;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications;

[TestFixture]
public class SearchUpdateServiceTests
{
    private Mock<IAvailableUpdatesLister> _mockAvailableUpdatesLister;
    private Mock<IEnvironmentService> _mockEnvironmentService;
    private Mock<IAvailableUpdateRepository> _mockAvailableUpdateRepository;
    private Mock<ILogger<SearchUpdateService>> _mockLogger;
    private SearchUpdateService _searchUpdateService;

    [SetUp]
    public void Setup()
    {
        _mockAvailableUpdatesLister = new Mock<IAvailableUpdatesLister>();
        _mockEnvironmentService = new Mock<IEnvironmentService>();
        _mockAvailableUpdateRepository = new Mock<IAvailableUpdateRepository>();
        _mockLogger = new Mock<ILogger<SearchUpdateService>>();

        _searchUpdateService = new SearchUpdateService(
            _mockAvailableUpdatesLister.Object,
            _mockEnvironmentService.Object,
            _mockAvailableUpdateRepository.Object,
            _mockLogger.Object
        );
    }
    
    [Test]
    public async Task SearchNextAvailableVersionsAsync_ShouldOrderByVersion()
    {
        // Arrange
        var currentVersion = new Version("1.0.0");
        _mockEnvironmentService.SetupGet(m => m.ApplicationVersion).Returns(currentVersion);

        var availableUpdates = new List<SoftwareVersion>
        {
            CreateSoftwareVersion("2.0.0", PriorityLevel.Optional),
            CreateSoftwareVersion("1.1.0", PriorityLevel.Recommended),
            CreateSoftwareVersion("1.0.1", PriorityLevel.Minimal)
        };

        _mockAvailableUpdatesLister.Setup(m => m.GetAvailableUpdates())
            .ReturnsAsync(availableUpdates);

        // Act
        await _searchUpdateService.SearchNextAvailableVersionsAsync();

        // Assert
        _mockAvailableUpdateRepository.Verify(m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(
            list => list.Count == 3 &&
                    list[0].Version == "1.0.1" && list[0].Level == PriorityLevel.Minimal &&
                    list[1].Version == "1.1.0" && list[1].Level == PriorityLevel.Recommended &&
                    list[2].Version == "2.0.0" && list[2].Level == PriorityLevel.Optional)), Times.Once);
        _mockAvailableUpdateRepository.Verify(
            m => m.Clear(), Times.Never);
    }

    [Test]
    public async Task SearchNextAvailableVersionsAsync_ShouldOrderByVersionThenByLevel()
    {
        // Arrange
        var currentVersion = new Version("1.0.0");
        _mockEnvironmentService.SetupGet(m => m.ApplicationVersion).Returns(currentVersion);

        var availableUpdates = new List<SoftwareVersion>
        {
            CreateSoftwareVersion("2.0.0", PriorityLevel.Optional),
            CreateSoftwareVersion("1.1.0", PriorityLevel.Recommended),
            CreateSoftwareVersion("1.1.0", PriorityLevel.Minimal)
        };

        _mockAvailableUpdatesLister.Setup(m => m.GetAvailableUpdates())
            .ReturnsAsync(availableUpdates);

        // Act
        await _searchUpdateService.SearchNextAvailableVersionsAsync();

        // Assert
        _mockAvailableUpdateRepository.Verify(m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(
            list => list.Count == 2 &&
                    list[0].Version == "1.1.0" && list[0].Level == PriorityLevel.Minimal &&
                    list[1].Version == "2.0.0" && list[1].Level == PriorityLevel.Optional)), Times.Once);
        _mockAvailableUpdateRepository.Verify(
            m => m.Clear(), Times.Never);
    }
    
    [Test]
    public async Task SearchNextAvailableVersionsAsync_ShouldOnlyRetainRelevantVersions()
    {
        // Arrange
        var currentVersion = new Version("1.1.0");
        _mockEnvironmentService.SetupGet(m => m.ApplicationVersion).Returns(currentVersion);

        var availableUpdates = new List<SoftwareVersion>
        {
            CreateSoftwareVersion("2.0.0", PriorityLevel.Optional),
            CreateSoftwareVersion("1.2.0", PriorityLevel.Recommended),
            CreateSoftwareVersion("1.0.0", PriorityLevel.Minimal)
        };

        _mockAvailableUpdatesLister.Setup(m => m.GetAvailableUpdates())
            .ReturnsAsync(availableUpdates);

        // Act
        await _searchUpdateService.SearchNextAvailableVersionsAsync();

        // Assert
        _mockAvailableUpdateRepository.Verify(m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(
            list => list.Count == 2 &&
                    list[0].Version == "1.2.0" && list[0].Level == PriorityLevel.Recommended &&
                    list[1].Version == "2.0.0" && list[1].Level == PriorityLevel.Optional)), Times.Once);
        _mockAvailableUpdateRepository.Verify(
            m => m.Clear(), Times.Never);
    }

    [Test]
    public async Task SearchNextAvailableVersionsAsync_ShouldDeduplicateVersions()
    {
        // Arrange
        var currentVersion = new Version("1.0.0");
        _mockEnvironmentService.SetupGet(m => m.ApplicationVersion).Returns(currentVersion);

        var availableUpdates = new List<SoftwareVersion>
        {
            CreateSoftwareVersion("1.1.0", PriorityLevel.Optional),
            CreateSoftwareVersion("1.1.0", PriorityLevel.Recommended),
            CreateSoftwareVersion("1.1.0", PriorityLevel.Minimal)
        };

        _mockAvailableUpdatesLister.Setup(m => m.GetAvailableUpdates())
            .ReturnsAsync(availableUpdates);

        // Act
        await _searchUpdateService.SearchNextAvailableVersionsAsync();

        // Assert
        _mockAvailableUpdateRepository.Verify(m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(
            list => list.Count == 1 &&
                    list[0].Version == "1.1.0" && list[0].Level == PriorityLevel.Minimal)), Times.Once);
        _mockAvailableUpdateRepository.Verify(
            m => m.Clear(), Times.Never);
    }
    
    [Theory]
    [TestCase(@"C:\Program Files\WindowsApps\MyApp.exe")]
    [TestCase(@"C:\Program Files (x86)\WindowsApps\MyApp.exe")]
    public async Task SearchNextAvailableVersionsAsync_WhenInstalledFromStore_ShouldUpdateWithEmptyList(string assemblyFullName)
    {
        // Arrange
        _mockEnvironmentService.SetupGet(m => m.AssemblyFullName)
            .Returns(@"C:\Program Files\WindowsApps\MyApp.exe");
        _mockEnvironmentService.SetupGet(m => m.OSPlatform)
            .Returns(OSPlatforms.Windows);

        // Act
        await _searchUpdateService.SearchNextAvailableVersionsAsync();

        // Assert
        _mockAvailableUpdateRepository.Verify(
            m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(list => list.Count == 0)),
            Times.Once
        );
        _mockAvailableUpdateRepository.Verify(
            m => m.UpdateAvailableUpdates(It.IsAny<List<SoftwareVersion>>()), Times.Once);
        _mockAvailableUpdateRepository.Verify(
            m => m.Clear(), Times.Never);
        _mockAvailableUpdatesLister.Verify(m => m.GetAvailableUpdates(), Times.Never);
    }

    private SoftwareVersion CreateSoftwareVersion(string version, PriorityLevel level)
    {
        return new SoftwareVersion
        {
            Version = version,
            Level = level,
            Files = new List<SoftwareVersionFile>()
        };
    }
}