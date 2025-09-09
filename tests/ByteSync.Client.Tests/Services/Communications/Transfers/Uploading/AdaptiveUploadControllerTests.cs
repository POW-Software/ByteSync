using ByteSync.Business.Sessions;
using ByteSync.Services.Communications.Transfers.Uploading;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using NUnit.Framework;
using Moq;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Common.Business.Sessions;

namespace ByteSync.Tests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class AdaptiveUploadControllerTests
{
    private AdaptiveUploadController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionObservable).Returns(System.Reactive.Linq.Observable.Return<AbstractSession?>(null));
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(System.Reactive.Linq.Observable.Never<SessionStatus>());
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
        _controller.RecordUploadResult(TimeSpan.FromSeconds(1), isSuccess: false, partNumber: 1, statusCode: 429);
        
        // Assert
        _controller.CurrentChunkSizeBytes.Should().Be(500 * 1024);
        _controller.CurrentParallelism.Should().Be(parallelismBefore);
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
            controller.RecordUploadResult(elapsed, isSuccess: successes, partNumber: i + 1);
        }
    }
}