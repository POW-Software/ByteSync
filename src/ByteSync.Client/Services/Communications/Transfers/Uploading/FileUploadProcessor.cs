using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FileUploadProcessor : IFileUploadProcessor
{
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly ILogger _logger;
    private readonly IFileUploadCoordinator _fileUploadCoordinator;
    private readonly IFileSlicer _fileSlicer;
    private readonly IFileUploadWorker _fileUploadWorker;
    private readonly IFilePartUploadAsserter _filePartUploadAsserter;
    private readonly string? _localFileToUpload;
    private readonly SemaphoreSlim _semaphoreSlim;

    // State tracking
    private UploadProgressState? _progressState;

    public FileUploadProcessor(
        ISlicerEncrypter slicerEncrypter,
        ILogger logger,
        IFileUploadCoordinator fileUploadCoordinator,
        IFileSlicer fileSlicer,
        IFileUploadWorker fileUploadWorker,
        IFilePartUploadAsserter filePartUploadAsserter,
        string? localFileToUpload,
        SemaphoreSlim semaphoreSlim)
    {
        _slicerEncrypter = slicerEncrypter;
        _logger = logger;
        _fileUploadCoordinator = fileUploadCoordinator;
        _fileSlicer = fileSlicer;
        _fileUploadWorker = fileUploadWorker;
        _filePartUploadAsserter = filePartUploadAsserter;
        _localFileToUpload = localFileToUpload;
        _semaphoreSlim = semaphoreSlim;
    }

    public async Task ProcessUpload(SharedFileDefinition sharedFileDefinition, int? maxSliceLength = null)
    {
        _progressState = new UploadProgressState();
        _progressState.StartTimeUtc = DateTimeOffset.UtcNow;
        
        // Start upload workers
        for (var i = 0; i < 2; i++)
        {
            _ = Task.Run(() => _fileUploadWorker.UploadAvailableSlicesAsync(_fileUploadCoordinator.AvailableSlices, _progressState));
        }
        
        // Start slicer
        await Task.Run(() => _fileSlicer.SliceAndEncryptAsync(sharedFileDefinition, _progressState, 
            maxSliceLength));
        
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