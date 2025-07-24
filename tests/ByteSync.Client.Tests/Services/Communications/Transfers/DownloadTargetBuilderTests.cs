using System.Reactive.Subjects;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Profiles;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications.Transfers;

[TestFixture]
public class DownloadTargetBuilderTests
{
    private Mock<ICloudSessionLocalDataManager> _cloudSessionLocalDataManager;
    private Mock<ISessionProfileLocalDataManager> _sessionProfileLocalDataManager;
    private Mock<ISharedActionsGroupRepository> _sharedActionsGroupRepository;
    private Mock<IConnectionService> _connectionService;
    private Mock<ITemporaryFileManagerFactory> _temporaryFileManagerFactory;
    private Mock<ISessionService> _sessionService;
    private Subject<AbstractSession?> _sessionSubject;
    private Subject<SessionStatus> _sessionStatusSubject;
    private DownloadTargetBuilder _downloadTargetBuilder;

    [SetUp]
    public void Setup()
    {
        _cloudSessionLocalDataManager = new Mock<ICloudSessionLocalDataManager>();
        _sessionProfileLocalDataManager = new Mock<ISessionProfileLocalDataManager>();
        _sharedActionsGroupRepository = new Mock<ISharedActionsGroupRepository>();
        _connectionService = new Mock<IConnectionService>();
        _temporaryFileManagerFactory = new Mock<ITemporaryFileManagerFactory>();
        _sessionService = new Mock<ISessionService>();
        
        _sessionSubject = new Subject<AbstractSession?>();
        _sessionStatusSubject = new Subject<SessionStatus>();
        
        _sessionService.Setup(s => s.SessionObservable).Returns(_sessionSubject);
        _sessionService.Setup(s => s.SessionStatusObservable).Returns(_sessionStatusSubject);
        
        _downloadTargetBuilder = new DownloadTargetBuilder(
            _cloudSessionLocalDataManager.Object,
            _sessionProfileLocalDataManager.Object,
            _sharedActionsGroupRepository.Object,
            _connectionService.Object,
            _temporaryFileManagerFactory.Object,
            _sessionService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _downloadTargetBuilder.Dispose();
        _sessionSubject.Dispose();
        _sessionStatusSubject.Dispose();
    }

    [Test]
    public void BuildDownloadTarget_ShouldCacheDownloadTarget()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-id",
            SharedFileType = SharedFileTypes.BaseInventory
        };
        
        _cloudSessionLocalDataManager.Setup(m => m.GetInventoryPath(sharedFileDefinition))
            .Returns("test-path");

        // Act
        var result1 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);
        var result2 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);

        // Assert
        result1.Should().BeSameAs(result2);
        result1.Should().NotBeNull();
    }

    [Test]
    public void ClearCache_ShouldRemoveCachedDownloadTarget()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-id",
            SharedFileType = SharedFileTypes.BaseInventory
        };
        
        _cloudSessionLocalDataManager.Setup(m => m.GetInventoryPath(sharedFileDefinition))
            .Returns("test-path");

        var result1 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);
        
        // Act
        _downloadTargetBuilder.ClearCache();
        var result2 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);

        // Assert
        result1.Should().NotBeSameAs(result2);
    }

    [Test]
    public void SessionEnd_ShouldAutomaticallyClearCache()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-id",
            SharedFileType = SharedFileTypes.BaseInventory
        };
        
        _cloudSessionLocalDataManager.Setup(m => m.GetInventoryPath(sharedFileDefinition))
            .Returns("test-path");

        var result1 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);
        
        // Act
        _sessionSubject.OnNext(null); // Simulate session end
        
        var result2 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);

        // Assert
        result1.Should().NotBeSameAs(result2);
    }

    [Test]
    public void SessionStatusPreparation_ShouldAutomaticallyClearCache()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-id",
            SharedFileType = SharedFileTypes.BaseInventory
        };
        
        _cloudSessionLocalDataManager.Setup(m => m.GetInventoryPath(sharedFileDefinition))
            .Returns("test-path");

        var result1 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);
        
        // Act
        _sessionStatusSubject.OnNext(SessionStatus.Preparation); // Simulate session reset
        
        var result2 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);

        // Assert
        result1.Should().NotBeSameAs(result2);
    }

    [Test]
    public void Dispose_ShouldClearCacheAndDisposeSubscriptions()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-id",
            SharedFileType = SharedFileTypes.BaseInventory
        };
        
        _cloudSessionLocalDataManager.Setup(m => m.GetInventoryPath(sharedFileDefinition))
            .Returns("test-path");

        var result1 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);
        
        // Act
        _downloadTargetBuilder.Dispose();
        var result2 = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);

        // Assert
        result1.Should().NotBeSameAs(result2);
        
        // Verify that subscriptions are disposed (no exceptions when publishing to disposed subjects)
        Assert.DoesNotThrow(() => _sessionSubject.OnNext(null));
        Assert.DoesNotThrow(() => _sessionStatusSubject.OnNext(SessionStatus.Preparation));
    }

    [Test]
    public void MultipleDisposeCalls_ShouldNotThrowException()
    {
        // Arrange & Act & Assert
        Assert.DoesNotThrow(() => _downloadTargetBuilder.Dispose());
        Assert.DoesNotThrow(() => _downloadTargetBuilder.Dispose());
        Assert.DoesNotThrow(() => _downloadTargetBuilder.Dispose());
    }

    [Test]
    public void BuildDownloadTarget_AfterDispose_ShouldStillWork()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-id",
            SharedFileType = SharedFileTypes.BaseInventory
        };
        
        _cloudSessionLocalDataManager.Setup(m => m.GetInventoryPath(sharedFileDefinition))
            .Returns("test-path");

        // Act
        _downloadTargetBuilder.Dispose();
        var result = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);

        // Assert
        result.Should().NotBeNull();
        result.SharedFileDefinition.Should().Be(sharedFileDefinition);
    }
} 