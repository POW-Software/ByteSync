using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FileUploadProcessor : IFileUploadProcessor
{
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly ILogger<FileUploadProcessor> _logger;
    private readonly IFileUploadCoordinator _fileUploadCoordinator;
    private readonly IUploadSlicingManager _uploadSlicingManager;
    private readonly IFileUploadWorker _fileUploadWorker;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ISessionService _sessionService;
    private readonly string? _localFileToUpload;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly IAdaptiveUploadController _adaptiveUploadController;
    private readonly SemaphoreSlim _uploadSlotsLimiter;
    private int _grantedSlots;
    private int _startedWorkers;
    private const int MAX_WORKERS = 4;
    private const int ADJUSTER_INTERVAL_MS = 200;
    private string? _sharedFileId;
    private readonly IInventoryService _inventoryService;

    // State tracking
    private UploadProgressState? _progressState;

    public FileUploadProcessor(
        ISlicerEncrypter slicerEncrypter,
        ILogger<FileUploadProcessor> logger,
        IFileUploadCoordinator fileUploadCoordinator,
        IFileUploadWorker fileUploadWorker,
        IFileTransferApiClient fileTransferApiClient,
        ISessionService sessionService,
        string? localFileToUpload,
        SemaphoreSlim semaphoreSlim,
        IAdaptiveUploadController adaptiveUploadController,
        IUploadSlicingManager uploadSlicingManager,
        SemaphoreSlim uploadSlotsLimiter,
        IInventoryService inventoryService)
    {
        _slicerEncrypter = slicerEncrypter;
        _logger = logger;
        _fileUploadCoordinator = fileUploadCoordinator;
        _fileUploadWorker = fileUploadWorker;
        _fileTransferApiClient = fileTransferApiClient;
        _sessionService = sessionService;
        _localFileToUpload = localFileToUpload;
        _semaphoreSlim = semaphoreSlim;
        _adaptiveUploadController = adaptiveUploadController;
        _uploadSlicingManager = uploadSlicingManager;
        _uploadSlotsLimiter = uploadSlotsLimiter;
        _inventoryService = inventoryService;
    }

    public async Task ProcessUpload(SharedFileDefinition sharedFileDefinition, int? maxSliceLength = null)
    {
        _sharedFileId = sharedFileDefinition.Id;
        _progressState = await _uploadSlicingManager.Enqueue(
            sharedFileDefinition,
            _slicerEncrypter,
            _fileUploadCoordinator.AvailableSlices,
            _semaphoreSlim,
            _fileUploadCoordinator.ExceptionOccurred);

        // Grant initial slots equal to current parallelism
        _grantedSlots = _adaptiveUploadController.CurrentParallelism;

        // Start initial workers equal to current parallelism (satisfies tests) and track how many we started
        var initialWorkers = Math.Clamp(_adaptiveUploadController.CurrentParallelism, 1, MAX_WORKERS);
        _startedWorkers = 0;
        for (var i = 0; i < initialWorkers; i++)
        {
            _startedWorkers++;
            _ = _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_fileUploadCoordinator.AvailableSlices, _progressState!);
        }

        // Start background adjuster to align available slots with desired parallelism
        long lastReported = 0;
        _ = Task.Run(async () =>
        {
            try
            {
                var finishedEvt = _fileUploadCoordinator.UploadingIsFinished;
                var errorEvt = _fileUploadCoordinator.ExceptionOccurred;

                while (!finishedEvt.WaitOne(0) && !errorEvt.WaitOne(0))
                {
                    var desiredRaw = _adaptiveUploadController.CurrentParallelism;
                    var desired = Math.Clamp(desiredRaw, 1, MAX_WORKERS);

                    AdjustSlots(desired);
                    EnsureWorkers(desired);

                    if (sharedFileDefinition.IsInventory && _progressState != null)
                    {
                        long current = 0;
                        await _semaphoreSlim.WaitAsync();
                        try
                        {
                            current = _progressState.TotalUploadedBytes;
                        }
                        finally
                        {
                            _semaphoreSlim.Release();
                        }

                        var delta = current - lastReported;
                        if (delta > 0)
                        {
                            _inventoryService.InventoryProcessData.UpdateMonitorData(m => { m.UploadedVolume += delta; });
                            lastReported = current;
                        }
                    }

                    await Task.Delay(ADJUSTER_INTERVAL_MS);
                }
            }
            catch
            {
                // no-op: adjuster is best-effort
            }
        });

        // Wait for completion
        await _fileUploadCoordinator.WaitForCompletionAsync();

        _slicerEncrypter.Dispose();

        if (sharedFileDefinition.IsInventory && _progressState != null)
        {
            long current;
            _semaphoreSlim.Wait();
            try
            {
                current = _progressState.TotalUploadedBytes;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
            var delta = current - lastReported;
            if (delta > 0)
            {
                _inventoryService.InventoryProcessData.UpdateMonitorData(m => { m.UploadedVolume += delta; });
                lastReported = current;
            }
        }

        if (_progressState.Exceptions.Count > 0)
        {
            var source = _localFileToUpload ?? "a stream";
            var lastException = _progressState.Exceptions[^1];

            throw new InvalidOperationException(
                $"An error occured while uploading '{source}' / sharedFileDefinition.Id:{sharedFileDefinition.Id}",
                lastException);
        }

        var totalCreatedSlices = GetTotalCreatedSlices();
        var sessionId = !string.IsNullOrWhiteSpace(_sessionService.SessionId)
            ? _sessionService.SessionId
            : sharedFileDefinition.SessionId;
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            TotalParts = totalCreatedSlices
        };
        await _fileTransferApiClient.AssertUploadIsFinished(transferParameters);

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
            Math.Round(totalBytes / 1024d),
            _progressState.MaxConcurrentUploads,
            bandwidthKbps,
            _progressState.Exceptions.Count);
    }

    private void AdjustSlots(int desired)
    {
        if (desired > _grantedSlots)
        {
            var prev = _grantedSlots;
            var diff = desired - _grantedSlots;
            try
            {
                _uploadSlotsLimiter.Release(diff);
            }
            catch
            {
                // ignored
            }

            _grantedSlots = desired;
            _logger.LogDebug("UploadAdjuster: file {FileId} slots increased {Prev}->{Now} (desired {Desired})", _sharedFileId, prev,
                _grantedSlots, desired);
        }
        else if (desired < _grantedSlots)
        {
            var prev = _grantedSlots;
            var toTake = _grantedSlots - desired;
            var taken = 0;
            for (int i = 0; i < toTake; i++)
            {
                if (_uploadSlotsLimiter.Wait(0))
                {
                    taken++;
                }
                else
                {
                    break;
                }
            }

            _grantedSlots -= taken;
            if (taken > 0)
            {
                _logger.LogDebug("UploadAdjuster: file {FileId} slots decreased {Prev}->{Now} (desired {Desired})", _sharedFileId, prev,
                    _grantedSlots, desired);
            }
            else
            {
                _logger.LogDebug(
                    "UploadAdjuster: file {FileId} requested decrease but no slot available to take (prev {Prev}, desired {Desired}, current {Current})",
                    _sharedFileId, prev, desired, _grantedSlots);
            }
        }
    }

    private void EnsureWorkers(int desired)
    {
        if (desired > _startedWorkers && _startedWorkers < MAX_WORKERS)
        {
            var toStart = Math.Min(desired - _startedWorkers, MAX_WORKERS - _startedWorkers);
            for (int i = 0; i < toStart; i++)
            {
                _startedWorkers++;
                _ = _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_fileUploadCoordinator.AvailableSlices, _progressState!);
            }

            _logger.LogDebug("UploadAdjuster: file {FileId} workers started +{Added}, total {Total}, desired {Desired}", _sharedFileId,
                toStart, _startedWorkers, desired);
        }
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
