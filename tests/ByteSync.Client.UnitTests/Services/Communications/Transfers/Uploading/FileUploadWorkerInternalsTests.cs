using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class FileUploadWorkerInternalsTests
{
    [Test]
    public void ComputeAttemptTimeoutSeconds_UsesFloorOf60Seconds_ForTinySlices()
    {
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(64 * 1024);
        
        timeout.Should().Be(FileUploadWorker.AttemptTimeoutFloorSeconds);
        timeout.Should().Be(60);
    }
    
    [Test]
    public void ComputeAttemptTimeoutSeconds_UsesFloor_For500KbSlices()
    {
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(500 * 1024);
        
        timeout.Should().Be(60);
    }
    
    [Test]
    public void ComputeAttemptTimeoutSeconds_UsesFloor_For1MbSlices()
    {
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(1024 * 1024);
        
        timeout.Should().Be(60);
    }
    
    [Test]
    public void ComputeAttemptTimeoutSeconds_ScalesLinearly_BeyondFloor()
    {
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(25L * 1024 * 1024);
        
        timeout.Should().Be(75);
    }
    
    [Test]
    public void ComputeAttemptTimeoutSeconds_CapsAtCeiling_ForLargeSlices()
    {
        var timeout = FileUploadWorker.ComputeAttemptTimeoutSeconds(64L * 1024 * 1024);
        
        timeout.Should().Be(FileUploadWorker.AttemptTimeoutCeilingSeconds);
        timeout.Should().Be(120);
    }
    
    [Test]
    public void RefineFailureKind_DoesNotRefine_NonClientCancellationKinds()
    {
        using var attemptCts = new CancellationTokenSource();
        
        FileUploadWorker.RefineFailureKind(UploadFailureKind.None, attemptCts, CancellationToken.None)
            .Should().Be(UploadFailureKind.None);
        FileUploadWorker.RefineFailureKind(UploadFailureKind.ServerError, attemptCts, CancellationToken.None)
            .Should().Be(UploadFailureKind.ServerError);
        FileUploadWorker.RefineFailureKind(UploadFailureKind.ClientTimeout, attemptCts, CancellationToken.None)
            .Should().Be(UploadFailureKind.ClientTimeout);
    }
    
    [Test]
    public void RefineFailureKind_ClientCancellation_WithGlobalCancelled_StaysCancellation()
    {
        using var global = new CancellationTokenSource();
        global.Cancel();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        
        var refined = FileUploadWorker.RefineFailureKind(UploadFailureKind.ClientCancellation, attemptCts, global.Token);
        
        refined.Should().Be(UploadFailureKind.ClientCancellation);
    }
    
    [Test]
    public void RefineFailureKind_ClientCancellation_WithAttemptCtsCancelled_BecomesTimeout()
    {
        using var global = new CancellationTokenSource();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        attemptCts.Cancel();
        
        var refined = FileUploadWorker.RefineFailureKind(UploadFailureKind.ClientCancellation, attemptCts, global.Token);
        
        refined.Should().Be(UploadFailureKind.ClientTimeout);
    }
    
    [Test]
    public void RefineFailureKind_ClientCancellation_NeitherTokenCancelled_StaysCancellation()
    {
        using var global = new CancellationTokenSource();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        
        var refined = FileUploadWorker.RefineFailureKind(UploadFailureKind.ClientCancellation, attemptCts, global.Token);
        
        refined.Should().Be(UploadFailureKind.ClientCancellation);
    }
    
    [Test]
    public void DetermineCancellationKind_GlobalCancelled_ReturnsClientCancellation()
    {
        using var global = new CancellationTokenSource();
        global.Cancel();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        
        var kind = FileUploadWorker.DetermineCancellationKind(attemptCts, global.Token);
        
        kind.Should().Be(UploadFailureKind.ClientCancellation);
    }
    
    [Test]
    public void DetermineCancellationKind_OnlyAttemptCancelled_ReturnsClientTimeout()
    {
        using var global = new CancellationTokenSource();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        attemptCts.Cancel();
        
        var kind = FileUploadWorker.DetermineCancellationKind(attemptCts, global.Token);
        
        kind.Should().Be(UploadFailureKind.ClientTimeout);
    }
    
    [Test]
    public void DetermineCancellationKind_NeitherCancelled_ReturnsClientCancellation()
    {
        using var global = new CancellationTokenSource();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(global.Token);
        
        var kind = FileUploadWorker.DetermineCancellationKind(attemptCts, global.Token);
        
        kind.Should().Be(UploadFailureKind.ClientCancellation);
    }
}
