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
        while (await availableSlices.Reader.WaitToReadAsync())
        {
            if (!availableSlices.Reader.TryRead(out var slice))
            {
                continue;
            }

            System.Diagnostics.Stopwatch? sliceStart = null;
            try
            {
                // Concurrency gating: acquire a slot for this upload
                await _uploadSlots.WaitAsync();

                sliceStart = System.Diagnostics.Stopwatch.StartNew();

                await IncrementConcurrentAsync(progressState);

                var response = await UploadSliceAndAssertAsync(slice);

                EnsureSuccessOrThrow(response);

                await UpdateProgressOnSuccessAsync(progressState, slice, sliceStart);
            }
            catch (Exception ex)
            {
                await HandleUploadExceptionAsync(progressState, ex);
                return;
            }
            finally
            {
                DisposeSlice(slice);
                await DecrementConcurrentAsync(progressState);
                try { _uploadSlots.Release(); } catch { }
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

    private async Task<UploadFileResponse?> UploadSliceAndAssertAsync(FileUploaderSlice slice)
    {
        var policy = _policyFactory.BuildFileUploadPolicy();
        var startedAt = DateTime.UtcNow;
        var response = await policy.ExecuteAsync(() => DoUpload(slice));

        var elapsed = DateTime.UtcNow - startedAt;
        _adaptiveUploadController.RecordUploadResult(
            elapsed,
            response != null && response.IsSuccess,
            slice.PartNumber,
            response?.StatusCode);

        if (response != null && response.IsSuccess)
        {
            var transferParameters = new TransferParameters
            {
                SessionId = _sharedFileDefinition.SessionId,
                SharedFileDefinition = _sharedFileDefinition,
                PartNumber = slice.PartNumber
            };

            await policy.ExecuteAsync(async () =>
            {
                await _fileTransferApiClient.AssertFilePartIsUploaded(transferParameters);
                return UploadFileResponse.Success(200);
            });
        }

        return response;
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
            var sliceBytes = slice.MemoryStream?.Length ?? 0L;
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

    private async Task HandleUploadExceptionAsync(UploadProgressState progressState, Exception ex)
    {
        _logger.LogError(ex, "UploadAvailableSlice");

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

    private static void DisposeSlice(FileUploaderSlice slice)
    {
        try { slice.MemoryStream?.Dispose(); } catch { }
    }

    private async Task CompleteIfFinishedAsync(UploadProgressState progressState)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (progressState.TotalUploadedSlices == progressState.TotalCreatedSlices)
            {
                _uploadingIsFinished.Set();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private async Task<UploadFileResponse> DoUpload(FileUploaderSlice slice)
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

            _logger.LogDebug("UploadAvailableSlice: starting sending slice {number} ({length} KB)",
                slice.PartNumber, slice.MemoryStream.Length / 1024d);

            var uploadStrategy = _strategies[uploadLocation.StorageProvider];
            var response = await uploadStrategy.UploadAsync(slice, uploadLocation, CancellationTokenSource.Token);

            return response;
        }
        catch (Exception)
        {
            _logger.LogError("Error while uploading slice {number}, sharedFileDefinitionId:{sharedFileDefinitionId} ",
                slice.PartNumber, _sharedFileDefinition.Id);
            throw;
        }
    }
}
