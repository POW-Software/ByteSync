using System;
using System.Reactive.Linq;
using ByteSync.Common.Business.Sessions;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Uploading;

[TestFixture]
public class AdaptiveUploadControllerTests
{
    private static IObservable<T> Empty<T>() => Observable.Empty<T>();

    private static AdaptiveUploadController CreateController()
    {
        var logger = new Mock<ILogger<AdaptiveUploadController>>().Object;
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionObservable).Returns(Empty<AbstractSession?>());
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Empty<SessionStatus>());
        return new AdaptiveUploadController(logger, sessionService.Object);
    }

    [Test]
    public void Initial_state_should_be_defaults()
    {
        var c = CreateController();
        c.CurrentChunkSizeBytes.Should().Be(500 * 1024);
        c.CurrentParallelism.Should().Be(2);
    }

    [Test]
    public void Upscale_should_increase_chunk_when_window_fast_and_successful()
    {
        var c = CreateController();
        var before = c.CurrentChunkSizeBytes;

        c.RecordUploadResult(TimeSpan.FromSeconds(5), true, partNumber: 1);
        c.RecordUploadResult(TimeSpan.FromSeconds(6), true, partNumber: 2);

        c.CurrentChunkSizeBytes.Should().BeGreaterThan(before);
        c.CurrentParallelism.Should().Be(2);
    }

    [Test]
    public void Downscale_should_reduce_chunk_when_slow_and_at_min_parallelism()
    {
        var c = CreateController();

        c.RecordUploadResult(TimeSpan.FromSeconds(35), true, partNumber: 1);
        c.RecordUploadResult(TimeSpan.FromSeconds(36), true, partNumber: 2);

        // 500 KB * 0.75 = 375 KB
        c.CurrentChunkSizeBytes.Should().Be(375 * 1024);
        c.CurrentParallelism.Should().Be(2);
    }

    [Test]
    public void Error_code_should_reset_chunk_to_initial()
    {
        var c = CreateController();

        c.RecordUploadResult(TimeSpan.FromSeconds(1), true, partNumber: 1);
        c.RecordUploadResult(TimeSpan.FromSeconds(1), true, partNumber: 2);
        c.CurrentChunkSizeBytes.Should().BeGreaterThan(500 * 1024);

        c.RecordUploadResult(TimeSpan.FromSeconds(1), false, partNumber: 3, statusCode: 429);
        c.CurrentChunkSizeBytes.Should().Be(500 * 1024);
    }

    [Test]
    public void Chunk_size_should_never_exceed_upper_bound()
    {
        var c = CreateController();
        for (int i = 0; i < 200; i++)
        {
            var p = c.CurrentParallelism;
            for (int j = 0; j < p; j++)
            {
                c.RecordUploadResult(TimeSpan.FromSeconds(1), true, partNumber: i * 10 + j);
            }
        }

        c.CurrentChunkSizeBytes.Should().BeLessThanOrEqualTo(16 * 1024 * 1024);
    }
}
