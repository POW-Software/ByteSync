using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Autofac.Features.Indexed;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using Polly.Retry;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FileUploadWorker : IFileUploadWorker
{
    private readonly IPolicyFactory _policyFactory;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ILogger<FileUploadWorker> _logger;
    private readonly SharedFileDefinition _sharedFileDefinition;
    private readonly ManualResetEvent _exceptionOccurred;
    private readonly ManualResetEvent _uploadingIsFinished;
    private readonly IIndex<StorageProvider, IUploadStrategy> _strategies;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly IAdaptiveUploadController _adaptiveUploadController;
    private readonly SemaphoreSlim _uploadSlots;
    
    private CancellationTokenSource CancellationTokenSource { get; }
    
    private static int _workerTaskCounter;
    private int _remainingSlicesDrained;
    
    public FileUploadWorker(
        IPolicyFactory policyFactory,
        IFileTransferApiClient fileTransferApiClient,
        SharedFileDefinition sharedFileDefinition,
        SemaphoreSlim semaphoreSlim,
        ManualResetEvent exceptionOccurred,
        IIndex<StorageProvider, IUploadStrategy> strategies,
        ManualResetEvent uploadingIsFinished,
        ILogger<FileUploadWorker> logger,
        IAdaptiveUploadController adaptiveUploadController,
        SemaphoreSlim uploadSlots)
    {
        _policyFactory = policyFactory;
        _fileTransferApiClient = fileTransferApiClient;
        _sharedFileDefinition = sharedFileDefinition;
        _semaphoreSlim = semaphoreSlim;
        _exceptionOccurred = exceptionOccurred;
        _uploadingIsFinished = uploadingIsFinished;
        _strategies = strategies;
        _logger = logger;
        _adaptiveUploadController = adaptiveUploadController;
        _uploadSlots = uploadSlots;
        CancellationTokenSource = new CancellationTokenSource();
    }
    
    // Backward-compatible overload for tests and callers not providing uploadSlots
    public FileUploadWorker(
        IPolicyFactory policyFactory,
        IFileTransferApiClient fileTransferApiClient,
        SharedFileDefinition sharedFileDefinition,
        SemaphoreSlim semaphoreSlim,
        ManualResetEvent exceptionOccurred,
        IIndex<StorageProvider, IUploadStrategy> strategies,
        ManualResetEvent uploadingIsFinished,
        ILogger<FileUploadWorker> logger,
        IAdaptiveUploadController adaptiveUploadController)
        : this(
            policyFactory,
            fileTransferApiClient,
            sharedFileDefinition,
            semaphoreSlim,
            exceptionOccurred,
            strategies,
            uploadingIsFinished,
            logger,
            adaptiveUploadController,
            new SemaphoreSlim(Math.Min(Math.Max(1, adaptiveUploadController.CurrentParallelism), 4), 4))
    {
    }
    
    public async Task UploadAvailableSlicesAdaptiveAsync(Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState)
    {
        var workerId = Interlocked.Increment(ref _workerTaskCounter);
        try
        {
            while (await availableSlices.Reader.WaitToReadAsync(CancellationTokenSource.Token))
            {
                if (_exceptionOccurred.WaitOne(0))
                {
                    return;
                }

                if (!availableSlices.Reader.TryRead(out var slice))
                {
                    continue;
                }

                try
                {
                    var sliceStart = Stopwatch.StartNew();

                    await IncrementConcurrentAsync(progressState);
                    var policy = _policyFactory.BuildFileUploadPolicy();
                    var attempt = 0;

                    var response = await policy.ExecuteAsync(async () =>
                    {
                        attempt++;

                        return await ExecuteUploadAttemptAsync(slice, workerId, attempt, CancellationTokenSource.Token);
                    });

                    if (!response.IsSuccess && CancellationTokenSource.IsCancellationRequested && _exceptionOccurred.WaitOne(0))
                    {
                        return;
                    }

                    EnsureSuccessOrThrow(response);

                    var fileName = _sharedFileDefinition.GetFileName(slice.PartNumber);
                    var assertSw = Stopwatch.StartNew();
                    _logger.LogDebug("UploadAvailableSlice: worker {WorkerId} start asserting slice {Number} for {FileName}",
                        workerId, slice.PartNumber, fileName);

                    var transferParameters = new TransferParameters
                    {
                        SessionId = _sharedFileDefinition.SessionId,
                        SharedFileDefinition = _sharedFileDefinition,
                        PartNumber = slice.PartNumber,
                        PartSizeInBytes = slice.MemoryStream.Length
                    };

                    await AssertSliceUploadedAsync(policy, transferParameters, workerId, slice.PartNumber, fileName, assertSw);
                    assertSw.Stop();
                    _logger.LogDebug(
                        "UploadAvailableSlice: worker {WorkerId} finished asserting slice {Number} for {FileName} in {ElapsedMs} ms",
                        workerId, slice.PartNumber, fileName, assertSw.ElapsedMilliseconds);

                    await UpdateProgressOnSuccessAsync(progressState, slice, sliceStart);
                }
                catch (OperationCanceledException) when (CancellationTokenSource.IsCancellationRequested && _exceptionOccurred.WaitOne(0))
                {
                    return;
                }
                catch (Exception ex)
                {
                    await HandleUploadExceptionAsync(progressState, availableSlices, ex, workerId);

                    return;
                }
                finally
                {
                    DisposeSlice(slice);
                    await DecrementConcurrentAsync(progressState);
                }
            }
        }
        catch (OperationCanceledException) when (CancellationTokenSource.IsCancellationRequested)
        {
            return;
        }
        
        await CompleteIfFinishedAsync(progressState);
    }
    
    private async Task<UploadFileResponse> ExecuteUploadAttemptAsync(FileUploaderSlice slice, int workerId, int attempt,
        CancellationToken globalToken)
    {
        var attemptStart = DateTime.UtcNow;
        var currentChunkSizeBytes = _adaptiveUploadController.CurrentChunkSizeBytes;
        var timeoutSec = UploadAttemptTimeoutPolicy.ComputeTimeoutSeconds(
            slice.MemoryStream.Length,
            attempt,
            currentChunkSizeBytes);
        var chunkRatio = currentChunkSizeBytes > 0
            ? slice.MemoryStream.Length / (double)currentChunkSizeBytes
            : 0d;
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
        attemptCts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));
        
        var beforeWait = _uploadSlots.CurrentCount;
        _logger.LogDebug(
            "UploadAvailableSlice: worker {WorkerId} waiting for upload slot (available {Available}), attempt {Attempt}, timeout {TimeoutSec}s, slice {SliceKb} KB, currentChunk {CurrentChunkKb} KB, chunkRatio {ChunkRatio}",
            workerId,
            beforeWait,
            attempt,
            timeoutSec,
            Math.Round(slice.MemoryStream.Length / 1024d),
            Math.Round(currentChunkSizeBytes / 1024d),
            Math.Round(chunkRatio, 2));
        
        var acquired = false;
        try
        {
            await _uploadSlots.WaitAsync(attemptCts.Token);
            acquired = true;
            
            var afterWait = _uploadSlots.CurrentCount;
            _logger.LogDebug("UploadAvailableSlice: worker {WorkerId} acquired upload slot (available now {Available}), attempt {Attempt}",
                workerId, afterWait, attempt);
            
            var uploadTask = DoUpload(slice, workerId, attemptCts.Token);
            var heartbeat = TimeSpan.FromSeconds(30);
            while (!uploadTask.IsCompleted)
            {
                var completed = await Task.WhenAny(uploadTask, Task.Delay(heartbeat, attemptCts.Token));
                if (completed == uploadTask)
                {
                    break;
                }
                
                var fileNameHb = _sharedFileDefinition.GetFileName(slice.PartNumber);
                _logger.LogDebug(
                    "UploadAvailableSlice: worker {WorkerId} uploading slice {Number} for {FileName}... attempt {Attempt}, elapsed {ElapsedMs} ms",
                    workerId, slice.PartNumber, fileNameHb, attempt, (DateTime.UtcNow - attemptStart).TotalMilliseconds);
                
                if (attemptCts.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        "UploadAvailableSlice: worker {WorkerId} upload attempt {Attempt} timed out after ~{TimeoutSec}s; waiting for cancellation...",
                        workerId, attempt, timeoutSec);
                    
                    break;
                }
            }
            
            var attemptResponse = await uploadTask;
            var elapsed = DateTime.UtcNow - attemptStart;
            var refinedKind = RefineFailureKind(attemptResponse.FailureKind, attemptCts, globalToken);
            _adaptiveUploadController.RecordUploadResult(new UploadResult(
                elapsed,
                attemptResponse.IsSuccess,
                slice.PartNumber,
                attemptResponse.StatusCode,
                FileId: _sharedFileDefinition.Id,
                ActualBytes: slice.MemoryStream.Length,
                FailureKind: refinedKind));
            
            return attemptResponse;
        }
        catch (OperationCanceledException oce)
        {
            var elapsed = DateTime.UtcNow - attemptStart;
            var kind = DetermineCancellationKind(attemptCts, globalToken);
            _adaptiveUploadController.RecordUploadResult(new UploadResult(
                elapsed,
                false,
                slice.PartNumber,
                Exception: oce,
                FileId: _sharedFileDefinition.Id,
                ActualBytes: slice.MemoryStream.Length,
                FailureKind: kind));
            
            throw new TaskCanceledException("Upload attempt canceled during slot wait or upload.", oce);
        }
        catch (Exception ex)
        {
            var elapsed = DateTime.UtcNow - attemptStart;
            _adaptiveUploadController.RecordUploadResult(new UploadResult(
                elapsed,
                false,
                slice.PartNumber,
                500,
                ex,
                _sharedFileDefinition.Id,
                slice.MemoryStream.Length,
                UploadFailureKind.ServerError));
            
            throw;
        }
        finally
        {
            try
            {
                if (acquired)
                {
                    var beforeRelease = _uploadSlots.CurrentCount;
                    _uploadSlots.Release();
                    var afterRelease = _uploadSlots.CurrentCount;
                    _logger.LogDebug(
                        "UploadAvailableSlice: worker {WorkerId} released upload slot after attempt {Attempt} (available {Before}->{After})",
                        workerId, attempt, beforeRelease, afterRelease);
                }
                else
                {
                    _logger.LogDebug(
                        "UploadAvailableSlice: worker {WorkerId} did not acquire upload slot (canceled before acquire) for attempt {Attempt}",
                        workerId, attempt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UploadAvailableSlice: worker {WorkerId} error releasing upload slot after attempt {Attempt}",
                    workerId, attempt);
            }
        }
    }
    
    private static UploadFailureKind RefineFailureKind(UploadFailureKind kind, CancellationTokenSource attemptCts,
        CancellationToken globalToken)
    {
        if (kind != UploadFailureKind.ClientCancellation)
        {
            return kind;
        }
        
        return DetermineCancellationKind(attemptCts, globalToken);
    }
    
    private static UploadFailureKind DetermineCancellationKind(CancellationTokenSource attemptCts, CancellationToken globalToken)
    {
        if (globalToken.IsCancellationRequested)
        {
            return UploadFailureKind.ClientCancellation;
        }
        
        if (attemptCts.IsCancellationRequested)
        {
            return UploadFailureKind.ClientTimeout;
        }
        
        return UploadFailureKind.ClientCancellation;
    }
    
    private async Task AssertSliceUploadedAsync(
        AsyncRetryPolicy<UploadFileResponse> policy,
        TransferParameters transferParameters,
        int workerId,
        int partNumber,
        string fileName,
        Stopwatch assertSw)
    {
        var assertTask = policy.ExecuteAsync(async () =>
        {
            await _fileTransferApiClient.AssertFilePartIsUploaded(transferParameters);
            
            return UploadFileResponse.Success(200);
        });
        
        while (!assertTask.IsCompleted)
        {
            var completed = await Task.WhenAny(assertTask, Task.Delay(TimeSpan.FromSeconds(30)));
            if (completed != assertTask)
            {
                _logger.LogDebug(
                    "UploadAvailableSlice: worker {WorkerId} asserting slice {Number} for {FileName}... elapsed {ElapsedMs} ms",
                    workerId, partNumber, fileName, assertSw.ElapsedMilliseconds);
            }
        }
        
        await assertTask;
    }
    
    private async Task IncrementConcurrentAsync(UploadProgressState progressState)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            progressState.ConcurrentUploads += 1;
            if (progressState.ConcurrentUploads > progressState.MaxConcurrentUploads)
            {
                progressState.MaxConcurrentUploads = progressState.ConcurrentUploads;
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
    private async Task DecrementConcurrentAsync(UploadProgressState progressState)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (progressState.ConcurrentUploads > 0)
            {
                progressState.ConcurrentUploads -= 1;
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
    private static void EnsureSuccessOrThrow(UploadFileResponse? response)
    {
        if (response == null || !response.IsSuccess)
        {
            throw new InvalidOperationException(
                $"UploadAvailableSlice: upload attempt failed. Status: {response?.StatusCode}, Error: {response?.ErrorMessage}");
        }
    }
    
    private async Task UpdateProgressOnSuccessAsync(UploadProgressState progressState, FileUploaderSlice slice, Stopwatch? sliceStart)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            progressState.TotalUploadedSlices += 1;
            var sliceBytes = slice.MemoryStream.Length;
            progressState.TotalUploadedBytes += sliceBytes;
            if (sliceStart != null)
            {
                progressState.LastSliceUploadDurationMs = sliceStart.ElapsedMilliseconds;
                progressState.LastSliceUploadedBytes = sliceBytes;
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
    private async Task HandleUploadExceptionAsync(
        UploadProgressState progressState,
        Channel<FileUploaderSlice> availableSlices,
        Exception ex,
        int workerId)
    {
        _logger.LogError(ex, "UploadAvailableSlice: worker {WorkerId} error", workerId);
        
        await _semaphoreSlim.WaitAsync();
        try
        {
            progressState.Exceptions.Add(ex);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
        
        _exceptionOccurred.Set();
        availableSlices.Writer.TryComplete(ex);
        await CancellationTokenSource.CancelAsync();
        DrainRemainingSlices(availableSlices.Reader);
    }

    private void DrainRemainingSlices(ChannelReader<FileUploaderSlice> reader)
    {
        if (Interlocked.Exchange(ref _remainingSlicesDrained, 1) == 1)
        {
            return;
        }

        while (reader.TryRead(out var slice))
        {
            DisposeSlice(slice);
        }
    }
    
    private void DisposeSlice(FileUploaderSlice slice)
    {
        try
        {
            slice.MemoryStream.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing slice {Number} memory stream", slice.PartNumber);
        }
    }
    
    private async Task CompleteIfFinishedAsync(UploadProgressState progressState)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (progressState.TotalUploadedSlices == progressState.TotalCreatedSlices)
            {
                _uploadingIsFinished.Set();
                _logger.LogDebug("UploadAvailableSlice: all slices uploaded ({Uploaded}/{Created}) - signaling completion",
                    progressState.TotalUploadedSlices, progressState.TotalCreatedSlices);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
    private async Task<UploadFileResponse> DoUpload(FileUploaderSlice slice, int workerId, CancellationToken cancellationToken)
    {
        try
        {
            var transferParameters = new TransferParameters
            {
                SessionId = _sharedFileDefinition.SessionId,
                SharedFileDefinition = _sharedFileDefinition,
                PartNumber = slice.PartNumber
            };
            
            var uploadLocation = await _fileTransferApiClient.GetUploadFileStorageLocation(transferParameters);
            var lengthKbRounded = (long)Math.Round((slice.MemoryStream.Length) / 1024d);
            var fileName = _sharedFileDefinition.GetFileName(slice.PartNumber);
            _logger.LogDebug("UploadAvailableSlice: worker {WorkerId} start uploading slice {Number} for {FileName} ({LengthKb} KB)",
                workerId, slice.PartNumber, fileName, lengthKbRounded);
            
            var uploadStrategy = _strategies[uploadLocation.StorageProvider];
            var sw = Stopwatch.StartNew();
            var response = await uploadStrategy.UploadAsync(slice, uploadLocation, cancellationToken);
            sw.Stop();
            _logger.LogDebug(
                "UploadAvailableSlice: worker {WorkerId} finished uploading slice {Number} for {FileName} ({LengthKb} KB) in {ElapsedMs} ms (status {Status})",
                workerId, slice.PartNumber, fileName, lengthKbRounded, sw.ElapsedMilliseconds, response.StatusCode);
            
            return response;
        }
        catch (Exception ex)
        {
            var fileName = _sharedFileDefinition.GetFileName(slice.PartNumber);
            _logger.LogError(ex,
                "Error while uploading slice {Number} for {FileName} (worker {WorkerId}), SharedFileDefinitionId:{SharedFileDefinitionId} ",
                slice.PartNumber, fileName, workerId, _sharedFileDefinition.Id);
            
            throw;
        }
    }
}