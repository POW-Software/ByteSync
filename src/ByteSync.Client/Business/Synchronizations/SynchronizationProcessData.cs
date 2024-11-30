using System.Reactive.Subjects;
using System.Threading.Tasks;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Business.Synchronizations;

public class SynchronizationProcessData
{
    public SynchronizationProcessData()
    {
        SynchronizationMainStatus = new ReplaySubject<SynchronizationProcessStatuses>(1);
        
        SynchronizationStart = new BehaviorSubject<Synchronization?>(null);
        SynchronizationAbortRequest = new BehaviorSubject<SynchronizationAbortRequest?>(null);
        SynchronizationEnd = new BehaviorSubject<SynchronizationEnd?>(null);
        SynchronizationDataTransmitted = new BehaviorSubject<bool>(false);
        
        SynchronizationProgress = new BehaviorSubject<SynchronizationProgress?>(null);
        
        Reset();
    }

    public ISubject<SynchronizationProcessStatuses> SynchronizationMainStatus { get; set; }
    
    public BehaviorSubject<Synchronization?> SynchronizationStart { get; set; }
    public BehaviorSubject<SynchronizationAbortRequest?> SynchronizationAbortRequest { get; set; }
    public BehaviorSubject<SynchronizationEnd?> SynchronizationEnd { get; set; }
    public BehaviorSubject<bool> SynchronizationDataTransmitted { get; set; }
    
    public BehaviorSubject<SynchronizationProgress?> SynchronizationProgress { get; set; }
    
    public long TotalVolumeToProcess { get; set; }
    public long TotalActionsToProcess { get; set; }

    public Task<bool> IsSynchronizationEnd()
    {
        return Task.FromResult(SynchronizationEnd.Value != null);
    }
    
    public Task<bool> IsSynchronizationRunning()
    {
        bool isSynchronizationRunning = SynchronizationStart.Value != null && SynchronizationEnd.Value == null;
        
        return Task.FromResult(isSynchronizationRunning);
    }

    public void Reset()
    {
        SynchronizationMainStatus.OnNext(SynchronizationProcessStatuses.Pending);
        
        SynchronizationStart.OnNext(null);
        SynchronizationAbortRequest.OnNext(null);
        SynchronizationEnd.OnNext(null);
        
        SynchronizationDataTransmitted.OnNext(false);
        
        SynchronizationProgress.OnNext(null);
    }
}