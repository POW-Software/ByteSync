using System.Reactive.Linq;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class AdaptiveUploadControllerTests
{
    private AdaptiveUploadController _controller = null!;
    
    [SetUp]
    public void SetUp()
    {
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionObservable).Returns(Observable.Return<AbstractSession?>(null));
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Never<SessionStatus>());
        _controller = new AdaptiveUploadController(NullLogger<AdaptiveUploadController>.Instance, sessionService.Object);
    }
    
    [Test]
    public void Initializes_WithDefaults()
    {
        // Arrange
        // (controller is created in SetUp)
        
        // Act
        // (no action)
        
        // Assert
        _controller.CurrentParallelism.Should().Be(2);
        _controller.CurrentChunkSizeBytes.Should().Be(500 * 1024);
    }
    
    [Test]
    public void Upscales_AfterFastWindow()
    {
        // Arrange
        var beforeSize = _controller.CurrentChunkSizeBytes;
        
        // Act - Two fast successful uploads fill the initial window
        FeedFastWindow(_controller);
        
        // Assert
        _controller.CurrentChunkSizeBytes.Should().BeGreaterThan(500 * 1024);
        _controller.CurrentChunkSizeBytes.Should().BeGreaterThan(beforeSize);
        _controller.CurrentParallelism.Should().Be(2);
    }
    
    [Test]
    public void IncreasesParallelism_AtFourMbAndEightMb()
    {
        // Arrange
        // (controller at default)
        
        // Act - Repeatedly feed fast windows until chunk size crosses 4MB
        var safety = 100;
        while (_controller.CurrentChunkSizeBytes < 4 * 1024 * 1024 && safety-- > 0)
        {
            FeedFastWindow(_controller);
        }
        
        // Assert - at 4MB threshold
        _controller.CurrentChunkSizeBytes.Should().BeGreaterThanOrEqualTo(4 * 1024 * 1024);
        _controller.CurrentParallelism.Should().BeGreaterThanOrEqualTo(3);
        
        // Act - Continue until 8MB threshold
        safety = 100;
        while (_controller.CurrentChunkSizeBytes < 8 * 1024 * 1024 && safety-- > 0)
        {
            FeedFastWindow(_controller);
        }
        
        // Assert - at 8MB threshold
        _controller.CurrentChunkSizeBytes.Should().BeGreaterThanOrEqualTo(8 * 1024 * 1024);
        _controller.CurrentParallelism.Should().BeGreaterThanOrEqualTo(4);
    }
    
    [Test]
    public void Downscale_ReducesParallelism_WhenAboveMin()
    {
        // Arrange: scale up to reach >=4MB so parallelism increases to at least 3
        var safety = 100;
        while (_controller.CurrentChunkSizeBytes < 4 * 1024 * 1024 && safety-- > 0)
        {
            FeedFastWindow(_controller);
        }
        
        _controller.CurrentParallelism.Should().BeGreaterThan(2);
        var beforeParallelism = _controller.CurrentParallelism;
        var beforeChunk = _controller.CurrentChunkSizeBytes;
        
        // Act: feed a slow window to trigger downscale-by-parallelism branch
        FeedWindow(_controller, TimeSpan.FromSeconds(31), successes: true);
        
        // Assert: parallelism decreased by 1, chunk size unchanged
        _controller.CurrentParallelism.Should().Be(beforeParallelism - 1);
        _controller.CurrentChunkSizeBytes.Should().Be(beforeChunk);
    }
    
    [Test]
    public void Downscale_ReducesChunkSize_WhenAtMinParallelism()
    {
        // Arrange
        _controller.CurrentParallelism.Should().Be(2);
        var before = _controller.CurrentChunkSizeBytes;
        
        // Act - Slow window triggers downscale
        FeedWindow(_controller, TimeSpan.FromSeconds(31), successes: true);
        
        // Assert - chunk size reduced but not below 64KB
        _controller.CurrentParallelism.Should().Be(2);
        _controller.CurrentChunkSizeBytes.Should().BeLessThan(before);
        _controller.CurrentChunkSizeBytes.Should().BeGreaterThanOrEqualTo(64 * 1024);
    }
    
    [Test]
    public void BandwidthErrors_ResetChunkSize()
    {
        // Arrange - Inflate chunk somewhat first
        FeedFastWindow(_controller);
        _controller.CurrentChunkSizeBytes.Should().BeGreaterThan(500 * 1024);
        var parallelismBefore = _controller.CurrentParallelism;
        
        // Act - Record bandwidth-related failure (e.g., 429)
        RecordUploadResult(_controller, TimeSpan.FromSeconds(1), isSuccess: false, partNumber: 1, statusCode: 429);
        
        // Assert
        _controller.CurrentChunkSizeBytes.Should().Be(500 * 1024);
        _controller.CurrentParallelism.Should().Be(parallelismBefore);
    }
    
    [Test]
    public void ClientCancellation_DoesNotResetChunkSize()
    {
        // Arrange - Inflate chunk somewhat first
        FeedFastWindow(_controller);
        var inflatedChunk = _controller.CurrentChunkSizeBytes;
        inflatedChunk.Should().BeGreaterThan(500 * 1024);
        var parallelismBefore = _controller.CurrentParallelism;
        
        // Act - Record a client cancellation (e.g., user pressed cancel)
        RecordUploadResult(
            _controller,
            TimeSpan.FromSeconds(2),
            isSuccess: false,
            partNumber: 1,
            statusCode: 0,
            failureKind: UploadFailureKind.ClientCancellation);
        
        // Assert
        _controller.CurrentChunkSizeBytes.Should().Be(inflatedChunk);
        _controller.CurrentParallelism.Should().Be(parallelismBefore);
    }
    
    [Test]
    public void ClientTimeout_DoesNotResetChunkSize()
    {
        // Arrange - Inflate chunk somewhat first
        FeedFastWindow(_controller);
        var inflatedChunk = _controller.CurrentChunkSizeBytes;
        inflatedChunk.Should().BeGreaterThan(500 * 1024);
        var parallelismBefore = _controller.CurrentParallelism;
        
        // Act - Record a client-side timeout (our attempt CTS expired)
        RecordUploadResult(
            _controller,
            TimeSpan.FromSeconds(60),
            isSuccess: false,
            partNumber: 1,
            statusCode: 0,
            failureKind: UploadFailureKind.ClientTimeout);
        
        // Assert
        _controller.CurrentChunkSizeBytes.Should().Be(inflatedChunk);
        _controller.CurrentParallelism.Should().Be(parallelismBefore);
    }
    
    [Test]
    public void ClientTimeout_DoesNotEnterAdaptiveWindow_AndDoesNotResetChunkSize()
    {
        // Arrange - Inflate chunk and make sure parallelism is just at min (=2)
        var safety = 10;
        while (_controller.CurrentChunkSizeBytes < 1024 * 1024 && safety-- > 0)
        {
            FeedFastWindow(_controller);
        }
        _controller.CurrentParallelism.Should().Be(2);
        var inflatedChunk = _controller.CurrentChunkSizeBytes;
        
        // Act - Feed a window of slow client-timeout failures (with the new failure kind)
        var p = _controller.CurrentParallelism;
        for (var i = 0; i < p; i++)
        {
            RecordUploadResult(
                _controller,
                TimeSpan.FromSeconds(60),
                isSuccess: false,
                partNumber: i + 1,
                statusCode: 0,
                failureKind: UploadFailureKind.ClientTimeout);
        }
        
        RecordUploadResult(
            _controller,
            TimeSpan.FromSeconds(1),
            isSuccess: true,
            partNumber: 100);
        
        // Assert - the timeout samples were ignored and cannot trigger a later downscale
        _controller.CurrentChunkSizeBytes.Should().NotBe(500 * 1024);
        _controller.CurrentChunkSizeBytes.Should().Be(inflatedChunk,
            because: "client-side cancellations are not bandwidth signals and must not influence chunk sizing");
    }
    
    [Test]
    public void ServerError500_StillResetsChunkSize_WhenNoFailureKind()
    {
        // Arrange - Inflate chunk somewhat first
        FeedFastWindow(_controller);
        _controller.CurrentChunkSizeBytes.Should().BeGreaterThan(500 * 1024);
        
        // Act - Record a real 500 server error (unknown failure kind)
        RecordUploadResult(_controller, TimeSpan.FromSeconds(2), isSuccess: false, partNumber: 1, statusCode: 500);
        
        // Assert - resets, like before
        _controller.CurrentChunkSizeBytes.Should().Be(500 * 1024);
    }
    
    private static void FeedFastWindow(AdaptiveUploadController controller)
    {
        FeedWindow(controller, TimeSpan.FromSeconds(10), successes: true);
    }
    
    private static void FeedWindow(AdaptiveUploadController controller, TimeSpan elapsed, bool successes)
    {
        var p = controller.CurrentParallelism;
        for (var i = 0; i < p; i++)
        {
            RecordUploadResult(controller, elapsed, isSuccess: successes, partNumber: i + 1);
        }
    }
    
    private static void RecordUploadResult(
        AdaptiveUploadController controller,
        TimeSpan elapsed,
        bool isSuccess,
        int partNumber,
        int? statusCode = null,
        UploadFailureKind failureKind = UploadFailureKind.None)
    {
        controller.RecordUploadResult(new UploadResult(
            elapsed,
            isSuccess,
            partNumber,
            statusCode,
            FailureKind: failureKind));
    }
}