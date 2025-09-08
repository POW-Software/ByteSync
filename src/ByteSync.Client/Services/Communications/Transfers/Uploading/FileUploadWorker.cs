using System.Threading;
using System.Threading.Channels;
using Autofac.Features.Indexed;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;

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
        while (await availableSlices.Reader.WaitToReadAsync())
        {
            if (!availableSlices.Reader.TryRead(out var slice))
            {
                continue;
            }

            try
            {
                var sliceStart = System.Diagnostics.Stopwatch.StartNew();

                await IncrementConcurrentAsync(progressState);
                var policy = _policyFactory.BuildFileUploadPolicy();
                var attempt = 0;
                // Upload with attempt-scoped slot gating and timeout per attempt
                var response = await policy.ExecuteAsync(async () =>
                {
                    attempt++;
                    // Acquire slot for this attempt only
                    var beforeWait = _uploadSlots.CurrentCount;
                    _logger.LogDebug("UploadAvailableSlice: worker {WorkerId} waiting for upload slot (available {Available})",
                        workerId, beforeWait);
                    await _uploadSlots.WaitAsync();
                    var afterWait = _uploadSlots.CurrentCount;
                    _logger.LogDebug("UploadAvailableSlice: worker {WorkerId} acquired upload slot (available now {Available}), attempt {Attempt}",
                        workerId, afterWait, attempt);

                    var attemptStart = DateTime.UtcNow;
                    // Compute attempt timeout based on slice size (3s/MB) bounded to [30s, 90s]
                    var sizeMb = Math.Max(1, (int)Math.Ceiling((slice.MemoryStream?.Length ?? 0L) / (1024d * 1024d)));
                    var timeoutSec = Math.Clamp(3 * sizeMb, 30, 90);
                    using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token);
                    attemptCts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

                    try
                    {
                        var uploadTask = DoUpload(slice, workerId, attemptCts.Token);
                        // Heartbeat while uploading (avoid using cancelled token with Delay to prevent tight loop)
                        var heartbeat = TimeSpan.FromSeconds(30);
                        while (!uploadTask.IsCompleted)
                        {
                            var completed = await Task.WhenAny(uploadTask, Task.Delay(heartbeat));
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
                        if (attemptResponse.IsSuccess)
                        {
                            _adaptiveUploadController.RecordUploadResult(elapsed, true, slice.PartNumber, attemptResponse.StatusCode, null, _sharedFileDefinition.Id, slice.MemoryStream?.Length ?? -1);
                        }
                        else
                        {
                            _adaptiveUploadController.RecordUploadResult(elapsed, false, slice.PartNumber, attemptResponse.StatusCode, null, _sharedFileDefinition.Id, slice.MemoryStream?.Length ?? -1);
                        }

                        return attemptResponse;
                    }
                    catch (Exception ex)
                    {
                        var elapsed = DateTime.UtcNow - attemptStart;
                        _adaptiveUploadController.RecordUploadResult(elapsed, false, slice.PartNumber, 500, ex, _sharedFileDefinition.Id, slice.MemoryStream?.Length ?? -1);
                        throw;
                    }
                    finally
                    {
                        // Release the slot at the end of this attempt
                        try
                        {
                            var beforeRelease = _uploadSlots.CurrentCount;
                            _uploadSlots.Release();
                            var afterRelease = _uploadSlots.CurrentCount;
                            _logger.LogDebug(
                                "UploadAvailableSlice: worker {WorkerId} released upload slot after attempt {Attempt} (available {Before}->{After})",
                                workerId, attempt, beforeRelease, afterRelease);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "UploadAvailableSlice: worker {WorkerId} error releasing upload slot after attempt {Attempt}",
                                workerId, attempt);
                        }
                    }
                });

                EnsureSuccessOrThrow(response);

                // 2) Assert on server (outside of upload slot)
                var fileName = _sharedFileDefinition.GetFileName(slice.PartNumber);
                var assertSw = System.Diagnostics.Stopwatch.StartNew();
                _logger.LogDebug("UploadAvailableSlice: worker {WorkerId} start asserting slice {Number} for {FileName}",
                    workerId, slice.PartNumber, fileName);

                var transferParameters = new TransferParameters
                {
                    SessionId = _sharedFileDefinition.SessionId,
                    SharedFileDefinition = _sharedFileDefinition,
                    PartNumber = slice.PartNumber
                };

                var assertTask = policy.ExecuteAsync(async () =>
                {
                    await _fileTransferApiClient.AssertFilePartIsUploaded(transferParameters);
                    return UploadFileResponse.Success(200);
                });

                // Heartbeat while asserting
                while (!assertTask.IsCompleted)
                {
                    var completed = await Task.WhenAny(assertTask, Task.Delay(TimeSpan.FromSeconds(30)));
                    if (completed != assertTask)
                    {
                        _logger.LogDebug(
                            "UploadAvailableSlice: worker {WorkerId} asserting slice {Number} for {FileName}... elapsed {ElapsedMs} ms",
                            workerId, slice.PartNumber, fileName, assertSw.ElapsedMilliseconds);
                    }
                }

                await assertTask;
                assertSw.Stop();
                _logger.LogDebug("UploadAvailableSlice: worker {WorkerId} finished asserting slice {Number} for {FileName} in {ElapsedMs} ms",
                    workerId, slice.PartNumber, fileName, assertSw.ElapsedMilliseconds);

                // Success path bookkeeping
                await UpdateProgressOnSuccessAsync(progressState, slice, sliceStart);
            }
            catch (Exception ex)
            {
                await HandleUploadExceptionAsync(progressState, ex, workerId);
                return;
            }
            finally
            {
                DisposeSlice(slice);
                await DecrementConcurrentAsync(progressState);
                // No final release here: attempts handled slot release per attempt
            }
        }

        await CompleteIfFinishedAsync(progressState);
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
            throw new Exception($"UploadAvailableSlice: unable to get upload url. Status: {response?.StatusCode}, Error: {response?.ErrorMessage}");
        }
    }

    private async Task UpdateProgressOnSuccessAsync(UploadProgressState progressState, FileUploaderSlice slice, System.Diagnostics.Stopwatch? sliceStart)
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

    private async Task HandleUploadExceptionAsync(UploadProgressState progressState, Exception ex, int workerId)
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await uploadStrategy.UploadAsync(slice, uploadLocation, cancellationToken);
            sw.Stop();
            _logger.LogDebug("UploadAvailableSlice: worker {WorkerId} finished uploading slice {Number} for {FileName} ({LengthKb} KB) in {ElapsedMs} ms (status {Status})",
                workerId, slice.PartNumber, fileName, lengthKbRounded, sw.ElapsedMilliseconds, response.StatusCode);

            return response;
        }
        catch (Exception ex)
        {
            var fileName = _sharedFileDefinition.GetFileName(slice.PartNumber);
            _logger.LogError(ex, "Error while uploading slice {Number} for {FileName} (worker {WorkerId}), sharedFileDefinitionId:{sharedFileDefinitionId} ",
                slice.PartNumber, fileName, workerId, _sharedFileDefinition.Id);
            throw;
        }
    }
}

