using System.Threading;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class UploadParallelismManager : IUploadParallelismManager
{
    private readonly IAdaptiveUploadController _adaptiveUploadController;
    private readonly IFileUploadWorker _fileUploadWorker;
    private readonly SemaphoreSlim _uploadSlotsLimiter;
    private readonly ILogger<UploadParallelismManager> _logger;
    private readonly string? _sharedFileId;
    
    private int _grantedSlots;
    private int _startedWorkers;
    
    private const int MAX_WORKERS = 4;
    
    public UploadParallelismManager(
        IAdaptiveUploadController adaptiveUploadController,
        IFileUploadWorker fileUploadWorker,
        SemaphoreSlim uploadSlotsLimiter,
        ILogger<UploadParallelismManager> logger,
        string? sharedFileId)
    {
        _adaptiveUploadController = adaptiveUploadController;
        _fileUploadWorker = fileUploadWorker;
        _uploadSlotsLimiter = uploadSlotsLimiter;
        _logger = logger;
        _sharedFileId = sharedFileId;
        _grantedSlots = 0;
        _startedWorkers = 0;
    }
    
    public void StartInitialWorkers(int count, Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState)
    {
        _startedWorkers = 0;
        for (var i = 0; i < count; i++)
        {
            _startedWorkers++;
            _ = _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(availableSlices, progressState);
        }
    }
    
    public void AdjustParallelism(int desired)
    {
        AdjustSlots(desired);
    }
    
    public int GetDesiredParallelism()
    {
        var desiredRaw = _adaptiveUploadController.CurrentParallelism;
        
        return Math.Clamp(desiredRaw, 1, MAX_WORKERS);
    }
    
    public int StartedWorkersCount => _startedWorkers;
    
    public void EnsureWorkers(int desired, Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState)
    {
        if (desired > _startedWorkers && _startedWorkers < MAX_WORKERS)
        {
            var toStart = Math.Min(desired - _startedWorkers, MAX_WORKERS - _startedWorkers);
            for (var i = 0; i < toStart; i++)
            {
                _startedWorkers++;
                _ = _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(availableSlices, progressState);
            }
            
            _logger.LogDebug("UploadAdjuster: file {FileId} workers started +{Added}, total {Total}, desired {Desired}",
                _sharedFileId, toStart, _startedWorkers, desired);
        }
    }
    
    public void SetGrantedSlots(int slots)
    {
        _grantedSlots = slots;
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
            _logger.LogDebug("UploadAdjuster: file {FileId} slots increased {Prev}->{Now} (desired {Desired})",
                _sharedFileId, prev, _grantedSlots, desired);
        }
        else if (desired < _grantedSlots)
        {
            var prev = _grantedSlots;
            var toTake = _grantedSlots - desired;
            var taken = 0;
            for (var i = 0; i < toTake; i++)
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
                _logger.LogDebug("UploadAdjuster: file {FileId} slots decreased {Prev}->{Now} (desired {Desired})",
                    _sharedFileId, prev, _grantedSlots, desired);
            }
            else
            {
                _logger.LogDebug(
                    "UploadAdjuster: file {FileId} requested decrease but no slot available to take (prev {Prev}, desired {Desired}, current {Current})",
                    _sharedFileId, prev, desired, _grantedSlots);
            }
        }
    }
}