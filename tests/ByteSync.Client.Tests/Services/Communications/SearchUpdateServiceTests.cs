using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;
using ByteSync.Services.Updates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications;

[TestFixture]
public class SearchUpdateServiceTests
{
    private Mock<IAvailableUpdatesLister> _mockAvailableUpdatesLister = null!;
    private Mock<IEnvironmentService> _mockEnvironmentService = null!;
    private Mock<IAvailableUpdateRepository> _mockAvailableUpdateRepository = null!;
    private TestLogger<SearchUpdateService> _testLogger = null!;
    private SearchUpdateService _searchUpdateService = null!;
    
    [SetUp]
    public void Setup()
    {
        _mockAvailableUpdatesLister = new Mock<IAvailableUpdatesLister>();
        _mockEnvironmentService = new Mock<IEnvironmentService>();
        _mockAvailableUpdateRepository = new Mock<IAvailableUpdateRepository>();
        _testLogger = new TestLogger<SearchUpdateService>();
        
        _searchUpdateService = new SearchUpdateService(
            _mockAvailableUpdatesLister.Object,
            _mockEnvironmentService.Object,
            _mockAvailableUpdateRepository.Object,
            _testLogger
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
        _mockAvailableUpdateRepository.Verify(m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(list => list.Count == 3 &&
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
        _mockAvailableUpdateRepository.Verify(m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(list => list.Count == 2 &&
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
        _mockAvailableUpdateRepository.Verify(m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(list => list.Count == 2 &&
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
        _mockAvailableUpdateRepository.Verify(m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(list => list.Count == 1 &&
            list[0].Version == "1.1.0" && list[0].Level == PriorityLevel.Minimal)), Times.Once);
        _mockAvailableUpdateRepository.Verify(
            m => m.Clear(), Times.Never);
    }
    
    [Theory]
    [TestCase(@"C:\\Program Files\\WindowsApps\\MyApp.exe")]
    [TestCase(@"C:\\Program Files (x86)\\WindowsApps\\MyApp.exe")]
    public async Task SearchNextAvailableVersionsAsync_WhenInstalledFromStore_ShouldSearchForUpdates(string assemblyFullName)
    {
        // Arrange
        var currentVersion = new Version("1.0.0");
        _mockEnvironmentService.SetupGet(m => m.ApplicationVersion).Returns(currentVersion);
        _mockEnvironmentService.SetupGet(m => m.AssemblyFullName).Returns(assemblyFullName);
        _mockEnvironmentService.SetupGet(m => m.OSPlatform).Returns(OSPlatforms.Windows);
        _mockEnvironmentService.SetupGet(m => m.DeploymentMode).Returns(DeploymentModes.MsixInstallation);
        
        var availableUpdates = new List<SoftwareVersion>
        {
            CreateSoftwareVersion("1.1.0", PriorityLevel.Minimal)
        };
        
        _mockAvailableUpdatesLister.Setup(m => m.GetAvailableUpdates())
            .ReturnsAsync(availableUpdates);
        
        // Act
        await _searchUpdateService.SearchNextAvailableVersionsAsync();
        
        // Assert
        _mockAvailableUpdateRepository.Verify(
            m => m.UpdateAvailableUpdates(It.Is<List<SoftwareVersion>>(list => list.Count == 1 && list[0].Version == "1.1.0")), Times.Once);
        _mockAvailableUpdateRepository.Verify(m => m.Clear(), Times.Never);
        _mockAvailableUpdatesLister.Verify(m => m.GetAvailableUpdates(), Times.Once);
        
        _testLogger.LogEntries.Should().Contain(entry =>
            entry.Level == LogLevel.Information &&
            entry.Message == "UpdateSystem: Application is installed from store, auto-update is disabled");
    }
    
    [Test]
    public async Task SearchNextAvailableVersionsAsync_WhenNoUpdatesFound_ShouldLogInformationAndPersistEmptyList()
    {
        // Arrange
        var currentVersion = new Version("2.0.0");
        _mockEnvironmentService.SetupGet(m => m.ApplicationVersion).Returns(currentVersion);
        
        var availableUpdates = new List<SoftwareVersion>
        {
            CreateSoftwareVersion("1.9.0", PriorityLevel.Minimal),
            CreateSoftwareVersion("2.0.0", PriorityLevel.Optional)
        };
        
        _mockAvailableUpdatesLister.Setup(m => m.GetAvailableUpdates())
            .ReturnsAsync(availableUpdates);
        
        List<SoftwareVersion>? persistedUpdates = null;
        _mockAvailableUpdateRepository.Setup(m => m.UpdateAvailableUpdates(It.IsAny<List<SoftwareVersion>>()))
            .Callback<List<SoftwareVersion>>(list => persistedUpdates = list);
        
        // Act
        await _searchUpdateService.SearchNextAvailableVersionsAsync();
        
        // Assert
        persistedUpdates.Should().NotBeNull();
        persistedUpdates!.Should().BeEmpty();
        
        _testLogger.LogEntries.Should().Contain(entry =>
            entry.Level == LogLevel.Information &&
            entry.Message == "UpdateSystem: No available update found");
    }
    
    [Test]
    public async Task SearchNextAvailableVersionsAsync_WhenListerThrows_ShouldClearRepositoryAndLogError()
    {
        // Arrange
        var currentVersion = new Version("1.0.0");
        _mockEnvironmentService.SetupGet(m => m.ApplicationVersion).Returns(currentVersion);
        
        _mockAvailableUpdatesLister.Setup(m => m.GetAvailableUpdates())
            .ThrowsAsync(new InvalidOperationException("failure"));
        
        // Act
        await _searchUpdateService.SearchNextAvailableVersionsAsync();
        
        // Assert
        _mockAvailableUpdateRepository.Verify(m => m.Clear(), Times.Once);
        _mockAvailableUpdateRepository.Verify(m => m.UpdateAvailableUpdates(It.IsAny<List<SoftwareVersion>>()), Times.Never);
        
        _testLogger.LogEntries.Should().Contain(entry =>
            entry.Level == LogLevel.Error &&
            entry.Message == "UpdateSystem" &&
            entry.Exception is InvalidOperationException);
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
    
    private sealed class TestLogger<T> : ILogger<T>
    {
        private static readonly NullScope _scope = new();
        private readonly List<(LogLevel Level, string Message, Exception? Exception)> _entries = new();
        
        public IReadOnlyList<(LogLevel Level, string Message, Exception? Exception)> LogEntries => _entries;
        
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return _scope;
        }
        
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string>? formatter)
        {
            var message = formatter != null ? formatter(state, exception) : state?.ToString() ?? string.Empty;
            _entries.Add((logLevel, message, exception));
        }
        
        private sealed class NullScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}