using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FileUploadProcessor : IFileUploadProcessor
{
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly ILogger<FileUploadProcessor> _logger;
    private readonly IFileUploadCoordinator _fileUploadCoordinator;
    private readonly IUploadSlicingManager _uploadSlicingManager;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ISessionService _sessionService;
    private readonly string? _localFileToUpload;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly IUploadParallelismManager _parallelismManager;
    private readonly IUploadProgressMonitor _progressMonitor;
    private readonly IInventoryService _inventoryService;
    
    private UploadProgressState? _progressState;
    
    public FileUploadProcessor(
        ISlicerEncrypter slicerEncrypter,
        ILogger<FileUploadProcessor> logger,
        IFileUploadCoordinator fileUploadCoordinator,
        IFileTransferApiClient fileTransferApiClient,
        ISessionService sessionService,
        string? localFileToUpload,
        SemaphoreSlim semaphoreSlim,
        IUploadSlicingManager uploadSlicingManager,
        IUploadParallelismManager parallelismManager,
        IUploadProgressMonitor progressMonitor,
        IInventoryService inventoryService)
    {
        _slicerEncrypter = slicerEncrypter;
        _logger = logger;
        _fileUploadCoordinator = fileUploadCoordinator;
        _fileTransferApiClient = fileTransferApiClient;
        _sessionService = sessionService;
        _localFileToUpload = localFileToUpload;
        _semaphoreSlim = semaphoreSlim;
        _uploadSlicingManager = uploadSlicingManager;
        _parallelismManager = parallelismManager;
        _progressMonitor = progressMonitor;
        _inventoryService = inventoryService;
    }
    
    public async Task ProcessUpload(SharedFileDefinition sharedFileDefinition, int? maxSliceLength = null)
    {
        await InitializeUploadAsync(sharedFileDefinition);
        
        var initialWorkers = _parallelismManager.GetDesiredParallelism();
        _parallelismManager.SetGrantedSlots(initialWorkers);
        _parallelismManager.StartInitialWorkers(initialWorkers, _fileUploadCoordinator.AvailableSlices, _progressState!);
        
        var lastReported = await _progressMonitor.MonitorProgressAsync(
            sharedFileDefinition,
            _progressState!,
            _parallelismManager,
            _fileUploadCoordinator.UploadingIsFinished,
            _fileUploadCoordinator.ExceptionOccurred,
            _semaphoreSlim);
        
        await FinalizeUploadAsync(sharedFileDefinition, lastReported);
        LogCompletion(sharedFileDefinition);
    }
    
    private async Task InitializeUploadAsync(SharedFileDefinition sharedFileDefinition)
    {
        _progressState = await _uploadSlicingManager.Enqueue(
            sharedFileDefinition,
            _slicerEncrypter,
            _fileUploadCoordinator.AvailableSlices,
            _semaphoreSlim,
            _fileUploadCoordinator.ExceptionOccurred);
    }
    
    
    private async Task FinalizeUploadAsync(SharedFileDefinition sharedFileDefinition, long lastReported)
    {
        await _fileUploadCoordinator.WaitForCompletionAsync();
        
        _slicerEncrypter.Dispose();
        
        if (sharedFileDefinition.IsInventory && _progressState != null)
        {
            var current = GetTotalUploadedBytes();
            var delta = current - lastReported;
            if (delta > 0)
            {
                _inventoryService.InventoryProcessData.UpdateMonitorData(m => { m.UploadedVolume += delta; });
            }
        }
        
        if (_progressState is { Exceptions.Count: > 0 })
        {
            var source = _localFileToUpload ?? "a stream";
            var lastException = _progressState.Exceptions[^1];
            
            throw new InvalidOperationException(
                $"An error occurred while uploading '{source}' / sharedFileDefinition.Id:{sharedFileDefinition.Id}",
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
    }
    
    private void LogCompletion(SharedFileDefinition sharedFileDefinition)
    {
        var durationMs = (_progressState!.EndTimeUtc - _progressState.StartTimeUtc)?.TotalMilliseconds;
        var totalBytes = GetTotalUploadedBytes();
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
    
    private long GetTotalUploadedBytes()
    {
        _semaphoreSlim.Wait();
        try
        {
            return _progressState?.TotalUploadedBytes ?? 0L;
        }
        finally
        {
            _semaphoreSlim.Release();
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