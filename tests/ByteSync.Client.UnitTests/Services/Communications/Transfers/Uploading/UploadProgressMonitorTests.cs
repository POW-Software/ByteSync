using System.Reactive.Linq;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class UploadProgressMonitorTests
{
    private Mock<IInventoryService> _mockInventoryService = null!;
    private Mock<ILogger<UploadProgressMonitor>> _mockLogger = null!;
    private Channel<FileUploaderSlice> _availableSlices = null!;
    private Mock<IUploadParallelismManager> _mockParallelismManager = null!;
    private ManualResetEvent _finishedEvent = null!;
    private ManualResetEvent _errorEvent = null!;
    private SemaphoreSlim _stateSemaphore = null!;
    private InventoryProcessData _inventoryProcessData = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockInventoryService = new Mock<IInventoryService>();
        _mockLogger = new Mock<ILogger<UploadProgressMonitor>>();
        _availableSlices = Channel.CreateBounded<FileUploaderSlice>(8);
        _mockParallelismManager = new Mock<IUploadParallelismManager>();
        _finishedEvent = new ManualResetEvent(false);
        _errorEvent = new ManualResetEvent(false);
        _stateSemaphore = new SemaphoreSlim(1, 1);
        _inventoryProcessData = new InventoryProcessData();
        
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(_inventoryProcessData);
    }
    
    [TearDown]
    public void TearDown()
    {
        _finishedEvent?.Dispose();
        _errorEvent?.Dispose();
        _stateSemaphore?.Dispose();
    }
    
    [Test]
    public async Task MonitorProgressAsync_ShouldCallParallelismManagerPeriodically()
    {
        // Arrange
        var monitor = CreateMonitor();
        var progressState = new UploadProgressState();
        var sharedFile = new SharedFileDefinition { Id = "test", SessionId = "session1" };
        _mockParallelismManager.Setup(x => x.GetDesiredParallelism()).Returns(2);
        
        var callCount = 0;
        _mockParallelismManager
            .Setup(x => x.AdjustParallelism(It.IsAny<int>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount >= 2)
                {
                    _finishedEvent.Set();
                }
            });
        
        // Act
        var result = await monitor.MonitorProgressAsync(
            sharedFile,
            progressState,
            _mockParallelismManager.Object,
            _finishedEvent,
            _errorEvent,
            _stateSemaphore);
        
        // Assert
        callCount.Should().BeGreaterThanOrEqualTo(2);
        _mockParallelismManager.Verify(x => x.GetDesiredParallelism(), Times.AtLeastOnce);
        _mockParallelismManager.Verify(x => x.AdjustParallelism(2), Times.AtLeastOnce);
    }
    
    [Test]
    public async Task MonitorProgressAsync_WithInventoryFile_ShouldUpdateInventoryData()
    {
        // Arrange
        var monitor = CreateMonitor();
        var progressState = new UploadProgressState { TotalUploadedBytes = 1000 };
        var sharedFile = new SharedFileDefinition
        {
            Id = "test",
            SessionId = "session1",
            SharedFileType = SharedFileTypes.BaseInventory
        };
        _mockParallelismManager.Setup(x => x.GetDesiredParallelism()).Returns(2);
        
        var callCount = 0;
        _mockParallelismManager
            .Setup(x => x.AdjustParallelism(It.IsAny<int>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount >= 3)
                {
                    _finishedEvent.Set();
                }
            });
        
        // Act
        await monitor.MonitorProgressAsync(
            sharedFile,
            progressState,
            _mockParallelismManager.Object,
            _finishedEvent,
            _errorEvent,
            _stateSemaphore);
        
        // Assert
        _inventoryProcessData.InventoryMonitorObservable.Take(1).Wait().UploadedVolume.Should().BeGreaterThan(0);
    }
    
    [Test]
    public async Task MonitorProgressAsync_WithNonInventoryFile_ShouldNotUpdateInventoryData()
    {
        // Arrange
        var monitor = CreateMonitor();
        var progressState = new UploadProgressState { TotalUploadedBytes = 1000 };
        var sharedFile = new SharedFileDefinition
        {
            Id = "test",
            SessionId = "session1",
            SharedFileType = SharedFileTypes.DeltaSynchronization
        };
        _mockParallelismManager.Setup(x => x.GetDesiredParallelism()).Returns(2);
        
        var callCount = 0;
        _mockParallelismManager
            .Setup(x => x.AdjustParallelism(It.IsAny<int>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount >= 2)
                {
                    _finishedEvent.Set();
                }
            });
        
        // Act
        await monitor.MonitorProgressAsync(
            sharedFile,
            progressState,
            _mockParallelismManager.Object,
            _finishedEvent,
            _errorEvent,
            _stateSemaphore);
        
        // Assert
        _inventoryProcessData.InventoryMonitorObservable.Take(1).Wait().UploadedVolume.Should().Be(0);
    }
    
    [Test]
    public async Task MonitorProgressAsync_WhenErrorOccurs_ShouldStopMonitoring()
    {
        // Arrange
        var monitor = CreateMonitor();
        var progressState = new UploadProgressState();
        var sharedFile = new SharedFileDefinition { Id = "test", SessionId = "session1" };
        _mockParallelismManager.Setup(x => x.GetDesiredParallelism()).Returns(2);
        
        _mockParallelismManager
            .Setup(x => x.AdjustParallelism(It.IsAny<int>()))
            .Callback(() => _errorEvent.Set());
        
        // Act
        var result = await monitor.MonitorProgressAsync(
            sharedFile,
            progressState,
            _mockParallelismManager.Object,
            _finishedEvent,
            _errorEvent,
            _stateSemaphore);
        
        // Assert
        result.Should().Be(0);
    }
    
    [Test]
    public async Task MonitorProgressAsync_ShouldReturnLastReportedBytes()
    {
        // Arrange
        var monitor = CreateMonitor();
        var progressState = new UploadProgressState { TotalUploadedBytes = 5000 };
        var sharedFile = new SharedFileDefinition
        {
            Id = "test",
            SessionId = "session1",
            SharedFileType = SharedFileTypes.BaseInventory
        };
        _mockParallelismManager.Setup(x => x.GetDesiredParallelism()).Returns(2);
        
        var callCount = 0;
        _mockParallelismManager
            .Setup(x => x.AdjustParallelism(It.IsAny<int>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount >= 2)
                {
                    _finishedEvent.Set();
                }
            });
        
        // Act
        var result = await monitor.MonitorProgressAsync(
            sharedFile,
            progressState,
            _mockParallelismManager.Object,
            _finishedEvent,
            _errorEvent,
            _stateSemaphore);
        
        // Assert
        result.Should().BeGreaterThan(0);
    }
    
    private UploadProgressMonitor CreateMonitor()
    {
        return new UploadProgressMonitor(
            _mockInventoryService.Object,
            _mockLogger.Object,
            _availableSlices);
    }
}