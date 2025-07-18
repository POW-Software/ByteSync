using System.Threading.Channels;
using System.Collections.Concurrent;
using NUnit.Framework;
using ByteSync.Services.Communications.Transfers;
using FluentAssertions;

namespace ByteSync.Tests.Services.Communications.Transfers;

public class ErrorManagerTests
{
    [Test]
    public async Task Constructor_InitializesWithNoError()
    {
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        var mergeChannel = Channel.CreateUnbounded<int>();
        var downloadQueue = new BlockingCollection<int>();
        var cts = new CancellationTokenSource();
        var manager = new ErrorManager(semaphoreSlim, mergeChannel, downloadQueue, cts);
        (await manager.IsErrorAsync()).Should().BeFalse();
    }

    [Test]
    public async Task SetOnError_SetsErrorAndCancels()
    {
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        var mergeChannel = Channel.CreateUnbounded<int>();
        var downloadQueue = new BlockingCollection<int>();
        var cts = new CancellationTokenSource();
        var manager = new ErrorManager(semaphoreSlim, mergeChannel, downloadQueue, cts);
        await manager.SetOnErrorAsync();
        (await manager.IsErrorAsync()).Should().BeTrue();
        cts.IsCancellationRequested.Should().BeTrue();
        ((System.Action)(() => downloadQueue.Add(1))).Should().Throw<InvalidOperationException>();
    }
} 