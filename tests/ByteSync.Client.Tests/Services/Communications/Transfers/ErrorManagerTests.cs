using System.Threading;
using System.Threading.Channels;
using System.Collections.Concurrent;
using NUnit.Framework;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Client.Tests.Services.Communications.Transfers;

public class ErrorManagerTests
{
    [Test]
    public void Constructor_InitializesWithNoError()
    {
        var syncRoot = new object();
        var mergeChannel = Channel.CreateUnbounded<int>();
        var downloadQueue = new BlockingCollection<int>();
        var cts = new CancellationTokenSource();
        var manager = new ErrorManager(syncRoot, mergeChannel, downloadQueue, cts);
        Assert.That(manager.IsError, Is.False);
    }

    [Test]
    public void SetOnError_SetsErrorAndCancels()
    {
        var syncRoot = new object();
        var mergeChannel = Channel.CreateUnbounded<int>();
        var downloadQueue = new BlockingCollection<int>();
        var cts = new CancellationTokenSource();
        var manager = new ErrorManager(syncRoot, mergeChannel, downloadQueue, cts);
        manager.SetOnError();
        Assert.That(manager.IsError, Is.True);
        Assert.That(cts.IsCancellationRequested, Is.True);
        Assert.That(() => downloadQueue.Add(1), Throws.TypeOf<InvalidOperationException>());
    }
} 