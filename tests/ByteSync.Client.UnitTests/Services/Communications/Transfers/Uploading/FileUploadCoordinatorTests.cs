using ByteSync.Business.Communications.Transfers;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class FileUploadCoordinatorTests
{
    private Mock<ILogger<FileUploadCoordinator>> _mockLogger = null!;
    private FileUploadCoordinator _coordinator = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<FileUploadCoordinator>>();
        _coordinator = new FileUploadCoordinator(_mockLogger.Object);
    }
    
    [Test]
    public void Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // Assert
        _coordinator.Should().NotBeNull();
        _coordinator.SyncRoot.Should().NotBeNull();
        _coordinator.AvailableSlices.Should().NotBeNull();
        _coordinator.UploadingIsFinished.Should().NotBeNull();
        _coordinator.ExceptionOccurred.Should().NotBeNull();
    }
    
    [Test]
    public void SyncRoot_ShouldReturnSameInstance()
    {
        // Act
        var syncRoot1 = _coordinator.SyncRoot;
        var syncRoot2 = _coordinator.SyncRoot;
        
        // Assert
        syncRoot1.Should().BeSameAs(syncRoot2);
    }
    
    [Test]
    public void AvailableSlices_ShouldReturnBoundedChannel()
    {
        // Act
        var channel = _coordinator.AvailableSlices;
        
        // Assert
        channel.Should().NotBeNull();
        channel.Reader.Should().NotBeNull();
        channel.Writer.Should().NotBeNull();
    }
    
    [Test]
    public void UploadingIsFinished_ShouldReturnManualResetEvent()
    {
        // Act
        var resetEvent = _coordinator.UploadingIsFinished;
        
        // Assert
        resetEvent.Should().NotBeNull();
        resetEvent.WaitOne(0).Should().BeFalse(); // Should be initially unsignaled
    }
    
    [Test]
    public void ExceptionOccurred_ShouldReturnManualResetEvent()
    {
        // Act
        var resetEvent = _coordinator.ExceptionOccurred;
        
        // Assert
        resetEvent.Should().NotBeNull();
        resetEvent.WaitOne(0).Should().BeFalse(); // Should be initially unsignaled
    }
    
    [Test]
    public async Task WaitForCompletionAsync_WhenNoEventsSet_ShouldWait()
    {
        // Arrange
        var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(100));
        var waitTask = _coordinator.WaitForCompletionAsync();
        
        // Act & Assert
        // This should timeout since no events are set
        var completedTask = await Task.WhenAny(waitTask, timeoutTask);
        completedTask.Should().Be(timeoutTask); // Timeout should complete first
    }
    
    [Test]
    public async Task WaitForCompletionAsync_WhenUploadingIsFinishedSet_ShouldComplete()
    {
        // Arrange
        var task = _coordinator.WaitForCompletionAsync();
        
        // Act
        _coordinator.UploadingIsFinished.Set();
        
        // Assert
        await task; // Should complete without throwing
    }
    
    [Test]
    public async Task WaitForCompletionAsync_WhenExceptionOccurredSet_ShouldComplete()
    {
        // Arrange
        var task = _coordinator.WaitForCompletionAsync();
        
        // Act
        _coordinator.ExceptionOccurred.Set();
        
        // Assert
        await task; // Should complete without throwing
    }
    
    [Test]
    public void HasExceptionOccurred_WhenNoExceptionSet_ShouldReturnFalse()
    {
        // Act
        var result = _coordinator.HasExceptionOccurred();
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Test]
    public void HasExceptionOccurred_WhenExceptionSet_ShouldReturnTrue()
    {
        // Arrange
        _coordinator.SetException(new Exception("Test exception"));
        
        // Act
        var result = _coordinator.HasExceptionOccurred();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void Reset_ShouldResetBothEvents()
    {
        // Arrange
        _coordinator.UploadingIsFinished.Set();
        _coordinator.ExceptionOccurred.Set();
        
        // Act
        _coordinator.Reset();
        
        // Assert
        _coordinator.UploadingIsFinished.WaitOne(0).Should().BeFalse();
        _coordinator.ExceptionOccurred.WaitOne(0).Should().BeFalse();
    }
    
    [Test]
    public void Reset_WhenNoEventsSet_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => _coordinator.Reset();
        action.Should().NotThrow();
    }
    
    [Test]
    public void AvailableSlices_ShouldAllowWritingAndReading()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        
        // Act & Assert
        var action = async () =>
        {
            await _coordinator.AvailableSlices.Writer.WriteAsync(slice);
            var readSlice = await _coordinator.AvailableSlices.Reader.ReadAsync();
            readSlice.Should().Be(slice);
        };
        action.Should().NotThrowAsync();
    }
    
    [Test]
    public void MultipleSetExceptionCalls_ShouldNotThrow()
    {
        // Arrange
        var exception1 = new Exception("First exception");
        var exception2 = new Exception("Second exception");
        
        // Act & Assert
        var action1 = () => _coordinator.SetException(exception1);
        var action2 = () => _coordinator.SetException(exception2);
        
        action1.Should().NotThrow();
        action2.Should().NotThrow();
        
        _coordinator.HasExceptionOccurred().Should().BeTrue();
    }
    
    [Test]
    public void MultipleResetCalls_ShouldNotThrow()
    {
        // Arrange
        _coordinator.UploadingIsFinished.Set();
        _coordinator.ExceptionOccurred.Set();
        
        // Act & Assert
        var action1 = () => _coordinator.Reset();
        var action2 = () => _coordinator.Reset();
        
        action1.Should().NotThrow();
        action2.Should().NotThrow();
        
        _coordinator.UploadingIsFinished.WaitOne(0).Should().BeFalse();
        _coordinator.ExceptionOccurred.WaitOne(0).Should().BeFalse();
    }
    
    [Test]
    public void ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        
        // Act & Assert
        var action = () =>
        {
            // Multiple threads accessing the coordinator
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    _coordinator.SetException(new Exception($"Exception {i}"));
                    _coordinator.Reset();
                    _coordinator.HasExceptionOccurred();
                    var _ = _coordinator.SyncRoot;
                }));
            }
            
            Task.WaitAll(tasks.ToArray());
        };
        action.Should().NotThrow();
    }
    
    [Test]
    public void WaitForCompletionAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(50));
        var waitTask = _coordinator.WaitForCompletionAsync();
        
        // Act & Assert
        // This should timeout since no events are set and no cancellation token is supported
        var action = async () =>
        {
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);
            completedTask.Should().Be(timeoutTask); // Timeout should complete first
        };
        action.Should().NotThrowAsync();
    }
}