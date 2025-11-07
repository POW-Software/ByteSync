using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class UploadParallelismManagerTests
{
    private Mock<IAdaptiveUploadController> _mockAdaptiveController = null!;
    private Mock<IFileUploadWorker> _mockWorker = null!;
    private Mock<ILogger<UploadParallelismManager>> _mockLogger = null!;
    private SemaphoreSlim _uploadSlotsLimiter = null!;
    private const string TestFileId = "test-file-id";
    
    [SetUp]
    public void SetUp()
    {
        _mockAdaptiveController = new Mock<IAdaptiveUploadController>();
        _mockWorker = new Mock<IFileUploadWorker>();
        _mockLogger = new Mock<ILogger<UploadParallelismManager>>();
        _uploadSlotsLimiter = new SemaphoreSlim(0, 4);
    }
    
    [TearDown]
    public void TearDown()
    {
        _uploadSlotsLimiter?.Dispose();
    }
    
    [Test]
    public void Constructor_ShouldInitializeWithZeroWorkers()
    {
        // Act
        var manager = CreateManager();
        
        // Assert
        manager.StartedWorkersCount.Should().Be(0);
    }
    
    [Test]
    public void GetDesiredParallelism_ShouldClampToMaxWorkers()
    {
        // Arrange
        _mockAdaptiveController.Setup(x => x.CurrentParallelism).Returns(10);
        var manager = CreateManager();
        
        // Act
        var result = manager.GetDesiredParallelism();
        
        // Assert
        result.Should().Be(4);
    }
    
    [Test]
    public void GetDesiredParallelism_ShouldClampToMinimumOne()
    {
        // Arrange
        _mockAdaptiveController.Setup(x => x.CurrentParallelism).Returns(0);
        var manager = CreateManager();
        
        // Act
        var result = manager.GetDesiredParallelism();
        
        // Assert
        result.Should().Be(1);
    }
    
    [Test]
    public void StartInitialWorkers_ShouldStartRequestedNumberOfWorkers()
    {
        // Arrange
        var manager = CreateManager();
        var channel = Channel.CreateBounded<FileUploaderSlice>(8);
        var progressState = new UploadProgressState();
        const int workerCount = 3;
        
        // Act
        manager.StartInitialWorkers(workerCount, channel, progressState);
        
        // Assert
        manager.StartedWorkersCount.Should().Be(workerCount);
        _mockWorker.Verify(
            x => x.UploadAvailableSlicesAdaptiveAsync(channel, progressState),
            Times.Exactly(workerCount));
    }
    
    [Test]
    public void AdjustParallelism_Increase_ShouldReleaseSlots()
    {
        // Arrange
        var manager = CreateManager();
        manager.SetGrantedSlots(0);
        
        // Act
        manager.AdjustParallelism(3);
        
        // Assert
        _uploadSlotsLimiter.CurrentCount.Should().Be(3);
    }
    
    [Test]
    public void AdjustParallelism_Decrease_ShouldTakeAvailableSlots()
    {
        // Arrange
        _uploadSlotsLimiter.Release(3);
        var manager = CreateManager();
        manager.SetGrantedSlots(3);
        
        // Act
        manager.AdjustParallelism(1);
        
        // Assert
        _uploadSlotsLimiter.CurrentCount.Should().Be(1);
    }
    
    [Test]
    public void EnsureWorkers_ShouldStartAdditionalWorkersWhenNeeded()
    {
        // Arrange
        var manager = CreateManager();
        var channel = Channel.CreateBounded<FileUploaderSlice>(8);
        var progressState = new UploadProgressState();
        
        manager.StartInitialWorkers(1, channel, progressState);
        _mockWorker.ResetCalls();
        
        // Act
        manager.EnsureWorkers(3, channel, progressState);
        
        // Assert
        manager.StartedWorkersCount.Should().Be(3);
        _mockWorker.Verify(
            x => x.UploadAvailableSlicesAdaptiveAsync(channel, progressState),
            Times.Exactly(2));
    }
    
    [Test]
    public void EnsureWorkers_ShouldNotExceedMaxWorkers()
    {
        // Arrange
        var manager = CreateManager();
        var channel = Channel.CreateBounded<FileUploaderSlice>(8);
        var progressState = new UploadProgressState();
        
        manager.StartInitialWorkers(4, channel, progressState);
        _mockWorker.ResetCalls();
        
        // Act
        manager.EnsureWorkers(10, channel, progressState);
        
        // Assert
        manager.StartedWorkersCount.Should().Be(4);
        _mockWorker.Verify(
            x => x.UploadAvailableSlicesAdaptiveAsync(channel, progressState),
            Times.Never);
    }
    
    [Test]
    public void SetGrantedSlots_ShouldUpdateInternalState()
    {
        // Arrange
        var manager = CreateManager();
        
        // Act
        manager.SetGrantedSlots(2);
        manager.AdjustParallelism(2);
        
        // Assert - no slots released because granted already matches desired
        _uploadSlotsLimiter.CurrentCount.Should().Be(0);
    }
    
    private UploadParallelismManager CreateManager()
    {
        return new UploadParallelismManager(
            _mockAdaptiveController.Object,
            _mockWorker.Object,
            _uploadSlotsLimiter,
            _mockLogger.Object,
            TestFileId);
    }
}