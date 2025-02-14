using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.Inventories;

public class InventoryProcessData : ReactiveObject
{
    public InventoryProcessData()
    {
        MainStatus = new ReplaySubject<LocalInventoryPartStatus>(1);
        IdentificationStatus = new ReplaySubject<LocalInventoryPartStatus>(1);
        AnalysisStatus = new ReplaySubject<LocalInventoryPartStatus>(1);

        // GlobalInventoryStatus = new ReplaySubject<GlobalInventoryStatus>(1);

        AreBaseInventoriesComplete = new ReplaySubject<bool>(1);
        AreFullInventoriesComplete = new ReplaySubject<bool>(1);

        InventoryMonitorDataSubject = new BehaviorSubject<InventoryMonitorData>(new InventoryMonitorData());
        
        // AllBaseInventoriesCompleteEvent = new ManualResetEvent(false);
        // AllFullInventoriesCompleteEvent = new ManualResetEvent(false);
        InventoryAbortionRequested = new ReplaySubject<bool>(1);
        
        ErrorEvent = new ReplaySubject<bool>(1);
        InventoryTransferError = new ReplaySubject<bool>(1);

        CancellationTokenSource = new CancellationTokenSource();

        /*
        Observable.CombineLatest(MainStatus, IdentificationStatus)
            .Select(l => (Main: l[0], Identification: l[1]))
            .Where(v => v.Main.In(LocalInventoryPartStatus.Error, LocalInventoryPartStatus.Cancelled)
                        && v.Identification == LocalInventoryPartStatus.Pending)
            .Subscribe(_ => IdentificationStatus = MainStatus);
        
        Observable.CombineLatest(MainStatus, AnalysisStatus)
            .Select(l => (Main: l[0], Analysis: l[1]))
            .Where(v => v.Main.In(LocalInventoryPartStatus.Error, LocalInventoryPartStatus.Cancelled)
                        && v.Analysis == LocalInventoryPartStatus.Pending)
            .Subscribe(_ => AnalysisStatus = MainStatus);
        */

        Observable.CombineLatest(MainStatus, IdentificationStatus)
            .Select(l => (Main: l[0], Identification: l[1]))
            .Where(v => v.Main.In(LocalInventoryPartStatus.Error, LocalInventoryPartStatus.Cancelled))
                .Subscribe(v =>
            {
                if (v.Identification == LocalInventoryPartStatus.Pending)
                {
                    IdentificationStatus.OnNext(LocalInventoryPartStatus.NotLaunched);
                }
                else if (v.Identification == LocalInventoryPartStatus.Running)
                {
                    IdentificationStatus.OnNext(LocalInventoryPartStatus.Cancelled);
                }
            });
        
        Observable.CombineLatest(MainStatus, AnalysisStatus)
            .Select(l => (Main: l[0], Analysis: l[1]))
            .Where(v => v.Main.In(LocalInventoryPartStatus.Error, LocalInventoryPartStatus.Cancelled))
            .Subscribe(v =>
        {
            if (v.Analysis == LocalInventoryPartStatus.Pending)
            {
                AnalysisStatus.OnNext(LocalInventoryPartStatus.NotLaunched);
            }
            else if (v.Analysis == LocalInventoryPartStatus.Running)
            {
                AnalysisStatus.OnNext(LocalInventoryPartStatus.Cancelled);
            }
        });
        
        Reset();

        //
        // ProcessStart = DateTime.Now;
        //
        // InventoryBuilderData = new InventoryBuilderData();
    }

    public List<IInventoryBuilder>? InventoryBuilders { get; set; }

    public List<Inventory>? Inventories
    {
        get
        {
            return InventoryBuilders?.Select(ib => ib.Inventory).ToList();
        }
    }
    
    // public ManualResetEvent AllBaseInventoriesCompleteEvent { get; }
    //
    // public ManualResetEvent AllFullInventoriesCompleteEvent { get; }
    //
    public CancellationTokenSource CancellationTokenSource { get; private set; }
    
    public ISubject<bool> InventoryAbortionRequested { get; }
    //
    public ISubject<bool> ErrorEvent { get; }
    
    public ISubject<bool> InventoryTransferError { get; }
    //
    // [Reactive]
    // public bool IsInventoryRunning { get; set; }
    //
    // [Reactive]
    // public bool IsIdentificationRunning { get; set; }
    //     
    // [Reactive]
    // public bool IsAnalysisRunning { get; set; }
    //     
    // [Reactive]
    // public bool HasInventoryStarted { get; set; }
    //     
    // [Reactive]
    // public bool HasAnalysisStarted { get; set; }
    
    public ISubject<LocalInventoryPartStatus> MainStatus { get; set; }
    
    public ISubject<LocalInventoryPartStatus> IdentificationStatus { get; set; }
    
    public ISubject<LocalInventoryPartStatus> AnalysisStatus { get; set; }
    
    // public ISubject<GlobalInventoryStatus> GlobalInventoryStatus { get; set; }
    
    public ISubject<bool> AreBaseInventoriesComplete { get; set; }
    
    public ISubject<bool> AreFullInventoriesComplete { get; set; }
    
    private BehaviorSubject<InventoryMonitorData> InventoryMonitorDataSubject { get; set; }
    
    public IObservable<InventoryMonitorData> InventoryMonitorObservable => InventoryMonitorDataSubject.AsObservable();

    // public InventoryMonitorData InventoryMonitor => InventoryMonitorDataSubject.Value;

    [Reactive]
    public DateTimeOffset InventoryStart  { get; set; }

    public Exception? LastException { get; set; }

    public void RequestInventoryAbort()
    {
        CancellationTokenSource.Cancel();
        InventoryAbortionRequested.OnNext(true);
    }
    
    public void Reset()
    {
        MainStatus.OnNext(LocalInventoryPartStatus.Pending);
        IdentificationStatus.OnNext(LocalInventoryPartStatus.Pending);
        AnalysisStatus.OnNext(LocalInventoryPartStatus.Pending);
        
        // GlobalInventoryStatus.OnNext(new GlobalInventoryStatus());

        AreBaseInventoriesComplete.OnNext(false);
        AreFullInventoriesComplete.OnNext(false);

        InventoryAbortionRequested.OnNext(false);
        CancellationTokenSource = new CancellationTokenSource();

        ErrorEvent.OnNext(false);
        InventoryTransferError.OnNext(false);

        LastException = null;
        
        InventoryMonitorDataSubject.OnNext(new InventoryMonitorData());
    }

    public void SetError(Exception exception)
    {
        LastException = exception;
        ErrorEvent.OnNext(true);
    }

    public void UpdateMonitorData(Action<InventoryMonitorData> action)
    {
        var newValue = InventoryMonitorDataSubject.Value;
        action.Invoke(newValue);
        
        InventoryMonitorDataSubject.OnNext(newValue);
    }
}