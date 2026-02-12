using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.Inventories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.Inventories;

public class InventoryProcessData : ReactiveObject
{
    private readonly object _monitorDataLock = new object();
    private readonly ConcurrentQueue<SkippedEntry> _skippedEntries = new();
    private readonly ConcurrentDictionary<SkipReason, int> _skippedCountsByReason = new();
    private int _skippedCount;
    
    public InventoryProcessData()
    {
        MainStatus = new ReplaySubject<InventoryTaskStatus>(1);
        GlobalMainStatus = new ReplaySubject<InventoryTaskStatus>(1);
        IdentificationStatus = new ReplaySubject<InventoryTaskStatus>(1);
        AnalysisStatus = new ReplaySubject<InventoryTaskStatus>(1);
        
        AreBaseInventoriesComplete = new ReplaySubject<bool>(1);
        AreFullInventoriesComplete = new ReplaySubject<bool>(1);
        
        InventoryMonitorDataSubject = new BehaviorSubject<InventoryMonitorData>(new InventoryMonitorData());
        
        InventoryAbortionRequested = new ReplaySubject<bool>(1);
        
        ErrorEvent = new ReplaySubject<bool>(1);
        InventoryTransferError = new ReplaySubject<bool>(1);
        
        CancellationTokenSource = new CancellationTokenSource();
        
        Observable.CombineLatest(MainStatus, IdentificationStatus)
            .Select(l => (Main: l[0], Identification: l[1]))
            .Where(v => v.Main.In(InventoryTaskStatus.Error, InventoryTaskStatus.Cancelled))
            .Subscribe(v =>
            {
                if (v.Identification == InventoryTaskStatus.Pending)
                {
                    IdentificationStatus.OnNext(InventoryTaskStatus.NotLaunched);
                }
                else if (v.Identification == InventoryTaskStatus.Running)
                {
                    IdentificationStatus.OnNext(v.Main == InventoryTaskStatus.Error
                        ? InventoryTaskStatus.Error
                        : InventoryTaskStatus.Cancelled);
                }
            });
        
        Observable.CombineLatest(MainStatus, AnalysisStatus)
            .Select(l => (Main: l[0], Analysis: l[1]))
            .Where(v => v.Main.In(InventoryTaskStatus.Error, InventoryTaskStatus.Cancelled))
            .Subscribe(v =>
            {
                if (v.Analysis == InventoryTaskStatus.Pending)
                {
                    AnalysisStatus.OnNext(InventoryTaskStatus.NotLaunched);
                }
                else if (v.Analysis == InventoryTaskStatus.Running)
                {
                    AnalysisStatus.OnNext(v.Main == InventoryTaskStatus.Error
                        ? InventoryTaskStatus.Error
                        : InventoryTaskStatus.Cancelled);
                }
            });
        
        Reset();
    }
    
    public List<IInventoryBuilder>? InventoryBuilders { get; set; }
    
    public List<Inventory>? Inventories
    {
        get { return InventoryBuilders?.Select(ib => ib.Inventory).ToList(); }
    }
    
    public CancellationTokenSource CancellationTokenSource { get; private set; }
    
    public ISubject<bool> InventoryAbortionRequested { get; }
    
    //
    public ISubject<bool> ErrorEvent { get; }
    
    public ISubject<bool> InventoryTransferError { get; }
    
    public ISubject<InventoryTaskStatus> MainStatus { get; set; }
    
    // Aggregated status across all DataMembers
    public ISubject<InventoryTaskStatus> GlobalMainStatus { get; set; }
    
    public ISubject<InventoryTaskStatus> IdentificationStatus { get; set; }
    
    public ISubject<InventoryTaskStatus> AnalysisStatus { get; set; }
    
    public ISubject<bool> AreBaseInventoriesComplete { get; set; }
    
    public ISubject<bool> AreFullInventoriesComplete { get; set; }
    
    private BehaviorSubject<InventoryMonitorData> InventoryMonitorDataSubject { get; set; }
    
    public IObservable<InventoryMonitorData> InventoryMonitorObservable => InventoryMonitorDataSubject.AsObservable();
    
    public IReadOnlyCollection<SkippedEntry> SkippedEntries => _skippedEntries.ToArray();
    
    public int SkippedCount => _skippedCount;
    
    [Reactive]
    public DateTimeOffset InventoryStart { get; set; }
    
    public Exception? LastException { get; set; }
    
    public void RequestInventoryAbort()
    {
        CancellationTokenSource.Cancel();
        InventoryAbortionRequested.OnNext(true);
    }
    
    public void Reset()
    {
        MainStatus.OnNext(InventoryTaskStatus.Pending);
        GlobalMainStatus.OnNext(InventoryTaskStatus.Pending);
        IdentificationStatus.OnNext(InventoryTaskStatus.Pending);
        AnalysisStatus.OnNext(InventoryTaskStatus.Pending);
        
        AreBaseInventoriesComplete.OnNext(false);
        AreFullInventoriesComplete.OnNext(false);
        
        InventoryAbortionRequested.OnNext(false);
        CancellationTokenSource = new CancellationTokenSource();
        
        ErrorEvent.OnNext(false);
        InventoryTransferError.OnNext(false);
        
        LastException = null;
        
        InventoryMonitorDataSubject.OnNext(new InventoryMonitorData());
        ClearSkippedEntries();
    }
    
    public void RecordSkippedEntry(SkippedEntry entry)
    {
        _skippedEntries.Enqueue(entry);
        _skippedCountsByReason.AddOrUpdate(entry.Reason, 1, (_, currentCount) => currentCount + 1);
        Interlocked.Increment(ref _skippedCount);
    }
    
    public int GetSkippedCountByReason(SkipReason reason)
    {
        return _skippedCountsByReason.TryGetValue(reason, out var count) ? count : 0;
    }

    public void SetError(Exception exception)
    {
        LastException = exception;
        ErrorEvent.OnNext(true);
    }
    
    public void UpdateMonitorData(Action<InventoryMonitorData> action)
    {
        lock (_monitorDataLock)
        {
            var currentValue = InventoryMonitorDataSubject.Value;
            var newValue = currentValue with { };
            action.Invoke(newValue);
            
            InventoryMonitorDataSubject.OnNext(newValue);
        }
    }
    
    private void ClearSkippedEntries()
    {
        while (_skippedEntries.TryDequeue(out _))
        {
        }
        
        _skippedCountsByReason.Clear();
        Interlocked.Exchange(ref _skippedCount, 0);
    }
}

