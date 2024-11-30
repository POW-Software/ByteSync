using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationService
{
    public SynchronizationProcessData SynchronizationProcessData { get; }
    
    // Task StartSynchronization(bool isLaunchedByUser);
    
    Task AbortSynchronization();
    
    // Task OnFileIsFullyDownloaded(LocalSharedFile downloadTargetLocalSharedFile);

    // Task SetSynchronizationStartData(SharedSynchronizationStartData synchronizationStartData);
    
    // Task OnSynchronizationAbortRequested(SynchronizationAbortRequest sar);
    
    Task OnSynchronizationStarted(Synchronization synchronization);
    
    Task OnSynchronizationUpdated(Synchronization synchronization);
    
    Task OnSynchronizationDataTransmitted(SharedSynchronizationStartData sharedSynchronizationStartData);
    
    // void UpdateProcessedVolume(long size);
    
    Task OnSynchronizationProgressChanged(SynchronizationProgressPush synchronizationProgressPush);
}