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
    public InventoryProcessData()
    {
        MainStatus = new ReplaySubject<LocalInventoryPartStatus>(1);
        IdentificationStatus = new ReplaySubject<LocalInventoryPartStatus>(1);
        AnalysisStatus = new ReplaySubject<LocalInventoryPartStatus>(1);

        AreBaseInventoriesComplete = new ReplaySubject<bool>(1);
        AreFullInventoriesComplete = new ReplaySubject<bool>(1);

        InventoryMonitorDataSubject = new BehaviorSubject<InventoryMonitorData>(new InventoryMonitorData());
        
        InventoryAbortionRequested = new ReplaySubject<bool>(1);
        
        ErrorEvent = new ReplaySubject<bool>(1);
        InventoryTransferError = new ReplaySubject<bool>(1);

        CancellationTokenSource = new CancellationTokenSource();

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
    }

    public List<IInventoryBuilder>? InventoryBuilders { get; set; }

    public List<Inventory>? Inventories
    {
        get
        {
            return InventoryBuilders?.Select(ib => ib.Inventory).ToList();
        }
    }
    
    public CancellationTokenSource CancellationTokenSource { get; private set; }
    
    public ISubject<bool> InventoryAbortionRequested { get; }
    //
    public ISubject<bool> ErrorEvent { get; }
    
    public ISubject<bool> InventoryTransferError { get; }
    
    public ISubject<LocalInventoryPartStatus> MainStatus { get; set; }
    
    public ISubject<LocalInventoryPartStatus> IdentificationStatus { get; set; }
    
    public ISubject<LocalInventoryPartStatus> AnalysisStatus { get; set; }
    
    public ISubject<bool> AreBaseInventoriesComplete { get; set; }
    
    public ISubject<bool> AreFullInventoriesComplete { get; set; }
    
    private BehaviorSubject<InventoryMonitorData> InventoryMonitorDataSubject { get; set; }
    
    public IObservable<InventoryMonitorData> InventoryMonitorObservable => InventoryMonitorDataSubject.AsObservable();

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