using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class FileUploadWorkerInternalsTests
{
    private static FileUploaderSlice SliceOfSize(int bytes)
    {
        return new FileUploaderSlice(1, new MemoryStream(new byte[bytes]));
    }
    
    [Test]
    public void ComputeAttemptTimeoutSeconds_UsesFloorOf60Seconds_ForTinySlices()
    {
        var slice = SliceOfSize(64 * 1024); // 64 KB
        
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(slice);
        
        timeout.Should().Be(FileUploadWorker.AttemptTimeoutFloorSeconds);
        timeout.Should().Be(60);
    }
    
    [Test]
    public void ComputeAttemptTimeoutSeconds_UsesFloor_For500KbSlices()
    {
        var slice = SliceOfSize(500 * 1024);
        
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(slice);
        
        timeout.Should().Be(60);
    }
    
    [Test]
    public void ComputeAttemptTimeoutSeconds_UsesFloor_For1MbSlices()
    {
        var slice = SliceOfSize(1024 * 1024);
        
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(slice);
        
        timeout.Should().Be(60);
    }
    
    [Test]
    public void ComputeAttemptTimeoutSeconds_ScalesLinearly_BeyondFloor()
    {
        var slice = SliceOfSize(25 * 1024 * 1024); // 25 MB -> 3*25 = 75s
        
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(slice);
        
        timeout.Should().Be(75);
    }
    
    [Test]
    public void ComputeAttemptTimeoutSeconds_CapsAtCeiling_ForLargeSlices()
    {
        var slice = SliceOfSize(64 * 1024 * 1024); // 64 MB -> would be 192s, capped
        
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(slice);
        
        timeout.Should().Be(FileUploadWorker.AttemptTimeoutCeilingSeconds);
        timeout.Should().Be(120);
    }
    
    [Test]
    public void RefineFailureKind_DoesNotRefine_NonClientCancellationKinds()
    {
        using var attemptCts = new CancellationTokenSource();
        
        FileUploadWorker.RefineFailureKind(UploadFailureKind.None, CancellationToken.None, attemptCts)
            .Should().Be(UploadFailureKind.None);
        FileUploadWorker.RefineFailureKind(UploadFailureKind.ServerError, CancellationToken.None, attemptCts)
            .Should().Be(UploadFailureKind.ServerError);
        FileUploadWorker.RefineFailureKind(UploadFailureKind.ClientTimeout, CancellationToken.None, attemptCts)
            .Should().Be(UploadFailureKind.ClientTimeout);
    }
    
    [Test]
    public void RefineFailureKind_ClientCancellation_WithGlobalCancelled_StaysCancellation()
    {
        using var global = new CancellationTokenSource();
        global.Cancel();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        
        var refined = FileUploadWorker.RefineFailureKind(UploadFailureKind.ClientCancellation, global.Token, attemptCts);
        
        refined.Should().Be(UploadFailureKind.ClientCancellation);
    }
    
    [Test]
    public void RefineFailureKind_ClientCancellation_WithAttemptCtsCancelled_BecomesTimeout()
    {
        using var global = new CancellationTokenSource();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        attemptCts.Cancel();
        
        var refined = FileUploadWorker.RefineFailureKind(UploadFailureKind.ClientCancellation, global.Token, attemptCts);
        
        refined.Should().Be(UploadFailureKind.ClientTimeout);
    }
    
    [Test]
    public void RefineFailureKind_ClientCancellation_NeitherTokenCancelled_StaysCancellation()
    {
        using var global = new CancellationTokenSource();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        
        var refined = FileUploadWorker.RefineFailureKind(UploadFailureKind.ClientCancellation, global.Token, attemptCts);
        
        refined.Should().Be(UploadFailureKind.ClientCancellation);
    }
    
    [Test]
    public void DetermineCancellationKind_GlobalCancelled_ReturnsClientCancellation()
    {
        using var global = new CancellationTokenSource();
        global.Cancel();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        
        var kind = FileUploadWorker.DetermineCancellationKind(global.Token, attemptCts);
        
        kind.Should().Be(UploadFailureKind.ClientCancellation);
    }
    
    [Test]
    public void DetermineCancellationKind_OnlyAttemptCancelled_ReturnsClientTimeout()
    {
        using var global = new CancellationTokenSource();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        attemptCts.Cancel();
        
        var kind = FileUploadWorker.DetermineCancellationKind(global.Token, attemptCts);
        
        kind.Should().Be(UploadFailureKind.ClientTimeout);
    }
    
    [Test]
    public void DetermineCancellationKind_NeitherCancelled_ReturnsClientCancellation()
    {
        using var global = new CancellationTokenSource();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        
        var kind = FileUploadWorker.DetermineCancellationKind(global.Token, attemptCts);
        
        kind.Should().Be(UploadFailureKind.ClientCancellation);
    }
}
