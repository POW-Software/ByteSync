using System.Threading;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Inventories;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class UploadProgressMonitor : IUploadProgressMonitor
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<UploadProgressMonitor> _logger;
    private readonly Channel<FileUploaderSlice> _availableSlices;
    
    private const int ADJUSTER_INTERVAL_MS = 200;
    
    public UploadProgressMonitor(
        IInventoryService inventoryService,
        ILogger<UploadProgressMonitor> logger,
        Channel<FileUploaderSlice> availableSlices)
    {
        _inventoryService = inventoryService;
        _logger = logger;
        _availableSlices = availableSlices;
    }
    
    public Task<long> MonitorProgressAsync(
        SharedFileDefinition sharedFileDefinition,
        UploadProgressState progressState,
        IUploadParallelismManager parallelismManager,
        ManualResetEvent finishedEvent,
        ManualResetEvent errorEvent,
        SemaphoreSlim stateSemaphore)
    {
        return Task.Run(async () =>
        {
            long lastReported = 0;
            try
            {
                while (!finishedEvent.WaitOne(0) && !errorEvent.WaitOne(0))
                {
                    var desired = parallelismManager.GetDesiredParallelism();
                    parallelismManager.AdjustParallelism(desired);
                    parallelismManager.EnsureWorkers(desired, _availableSlices, progressState);
                    
                    if (sharedFileDefinition.IsInventory)
                    {
                        var current = GetTotalUploadedBytes(progressState, stateSemaphore);
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UploadProgressMonitor: Exception in monitoring loop");
            }
            
            return lastReported;
        });
    }
    
    private static long GetTotalUploadedBytes(UploadProgressState progressState, SemaphoreSlim stateSemaphore)
    {
        stateSemaphore.Wait();
        try
        {
            return progressState.TotalUploadedBytes;
        }
        finally
        {
            stateSemaphore.Release();
        }
    }
}