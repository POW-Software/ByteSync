using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryAnalysisViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryAnalysisViewModel()
    {
        
    } 
    
    public InventoryAnalysisViewModel(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
        
        this.WhenActivated(HandleActivation);
    }

    private void HandleActivation(CompositeDisposable disposables)
    {
        _inventoryService.InventoryProcessData.AnalysisStatus
            .ToPropertyEx(this, x => x.AnalysisStatus)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.AnalysisStatus
            .Select(ms => ms == LocalInventoryPartStatus.Running)
            .ToPropertyEx(this, x => x.IsAnalysisRunning)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.AnalysisStatus
            .Select(ms => ms == LocalInventoryPartStatus.Running && ! HasAnalysisStarted)
            .Where(b => b is true)
            .ToPropertyEx(this, x => x.HasAnalysisStarted)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.InventoryMonitorObservable
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(m =>
            {
                AnalyzeErrors = m.AnalyzeErrors;
                AnalyzedFiles = m.AnalyzedFiles;
                AnalyzableFiles = m.AnalyzableFiles;
                ProcessedSize = m.ProcessedSize;
            })
            .DisposeWith(disposables);
    }
    
    public extern LocalInventoryPartStatus AnalysisStatus { [ObservableAsProperty] get; }
    
    public extern bool IsAnalysisRunning { [ObservableAsProperty] get; }
    
    public extern bool HasAnalysisStarted { [ObservableAsProperty] get; }
    
    [Reactive]
    public int AnalyzeErrors { get; set; }
    
    [Reactive]
    public int AnalyzedFiles { get; set; }
    
    [Reactive]
    public long AnalyzableFiles { get; set; }
    
    public long ProcessedSize { get; set; }
}