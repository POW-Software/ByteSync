using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FileUploadProcessor : IFileUploadProcessor
{
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly ILogger<FileUploadProcessor> _logger;
    private readonly IFileUploadCoordinator _fileUploadCoordinator;
    private readonly IUploadSlicingManager _uploadSlicingManager;
    private readonly IFileUploadWorker _fileUploadWorker;
    private readonly IFilePartUploadAsserter _filePartUploadAsserter;
    private readonly string? _localFileToUpload;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly IAdaptiveUploadController _adaptiveUploadController;

    // State tracking
    private UploadProgressState? _progressState;

    public FileUploadProcessor(
        ISlicerEncrypter slicerEncrypter,
        ILogger<FileUploadProcessor> logger,
        IFileUploadCoordinator fileUploadCoordinator,
        IFileUploadWorker fileUploadWorker,
        IFilePartUploadAsserter filePartUploadAsserter,
        string? localFileToUpload,
        SemaphoreSlim semaphoreSlim,
        IAdaptiveUploadController adaptiveUploadController,
        IUploadSlicingManager uploadSlicingManager)
    {
        _slicerEncrypter = slicerEncrypter;
        _logger = logger;
        _fileUploadCoordinator = fileUploadCoordinator;
        _fileUploadWorker = fileUploadWorker;
        _filePartUploadAsserter = filePartUploadAsserter;
        _localFileToUpload = localFileToUpload;
        _semaphoreSlim = semaphoreSlim;
        _adaptiveUploadController = adaptiveUploadController;
        _uploadSlicingManager = uploadSlicingManager;
    }

    public async Task ProcessUpload(SharedFileDefinition sharedFileDefinition, int? maxSliceLength = null)
    {
        _progressState = await _uploadSlicingManager.Enqueue(
            sharedFileDefinition,
            _slicerEncrypter,
            _fileUploadCoordinator.AvailableSlices,
            _semaphoreSlim,
            _fileUploadCoordinator.ExceptionOccurred,
            _adaptiveUploadController);

        // Start upload workers (adaptive)
        for (var i = 0; i < _adaptiveUploadController.CurrentParallelism; i++)
        {
            _ = Task.Run(() => _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_fileUploadCoordinator.AvailableSlices, _progressState!));
        }

        // Wait for completion
        await _fileUploadCoordinator.WaitForCompletionAsync();

        _slicerEncrypter.Dispose();

        if (_progressState.Exceptions != null && _progressState.Exceptions.Count > 0)
        {
            var source = _localFileToUpload ?? "a stream";
            var lastException = _progressState.Exceptions[_progressState.Exceptions.Count - 1];
            throw new InvalidOperationException($"An error occured while uploading '{source}' / sharedFileDefinition.Id:{sharedFileDefinition.Id}",
                lastException);
        }

        var totalCreatedSlices = GetTotalCreatedSlices();
        await _filePartUploadAsserter.AssertUploadIsFinished(sharedFileDefinition, totalCreatedSlices);
        
        _progressState.EndTimeUtc = DateTimeOffset.UtcNow;
        var durationMs = (_progressState.EndTimeUtc - _progressState.StartTimeUtc)?.TotalMilliseconds;
        var totalBytes = _progressState.TotalUploadedBytes;
        var bandwidthKbps = durationMs.HasValue && durationMs.Value > 0
            ? (totalBytes * 8.0) / durationMs.Value
            : 0.0;
        _logger.LogInformation(
            "FileUploadProcessor: E2EE upload of {SharedFileDefinitionId} is finished in {DurationMs} ms, uploaded {UploadedKB} KB, max concurrency {MaxConc}, approx bandwidth {Kbps:F2} kbps, {Errors} error(s)",
            sharedFileDefinition.Id,
            durationMs,
            totalBytes / 1024d,
            _progressState.MaxConcurrentUploads,
            bandwidthKbps,
            _progressState.Exceptions!.Count);
    }

    public int GetTotalCreatedSlices()
    {
        _semaphoreSlim.Wait();
        try
        {
            return _progressState?.TotalCreatedSlices ?? 0;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public int GetMaxConcurrentUploads()
    {
        _semaphoreSlim.Wait();
        try
        {
            return _progressState?.MaxConcurrentUploads ?? 0;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
} 