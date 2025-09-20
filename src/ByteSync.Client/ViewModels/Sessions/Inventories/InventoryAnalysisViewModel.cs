using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryAnalysisViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IThemeService _themeService = null!;
    
    public InventoryAnalysisViewModel()
    {
    }
    
    public InventoryAnalysisViewModel(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
        
        this.WhenActivated(HandleActivation);
    }
    
    public InventoryAnalysisViewModel(IInventoryService inventoryService, IThemeService themeService)
        : this(inventoryService)
    {
        _themeService = themeService;
        AnalysisIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
    }
    
    // Keep this view focused on local analysis only.
    
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
            .Select(ms => ms == LocalInventoryPartStatus.Running && !HasAnalysisStarted)
            .Where(b => b is true)
            .ToPropertyEx(this, x => x.HasAnalysisStarted)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.InventoryMonitorObservable
            .Sample(TimeSpan.FromMilliseconds(500))
            .Subscribe(m =>
            {
                AnalyzeErrors = m.AnalyzeErrors;
                AnalyzedFiles = m.AnalyzedFiles;
                AnalyzableFiles = m.AnalyzableFiles;
                ProcessedSize = m.ProcessedSize;
            })
            .DisposeWith(disposables);
        
        this.WhenAnyValue(x => x.AnalyzeErrors)
            .Select(e => e > 0)
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.HasErrors)
            .DisposeWith(disposables);
        
        this.WhenAnyValue(x => x.AnalysisStatus, x => x.AnalyzeErrors)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(tuple =>
            {
                var status = tuple.Item1;
                var errors = tuple.Item2;
                
                switch (status)
                {
                    case LocalInventoryPartStatus.Error:
                    case LocalInventoryPartStatus.Cancelled:
                    case LocalInventoryPartStatus.NotLaunched:
                        AnalysisIcon = "SolidXCircle";
                        AnalysisIconBrush = _themeService?.GetBrush("MainSecondaryColor");
                        
                        break;
                    case LocalInventoryPartStatus.Success:
                        AnalysisIcon = errors > 0 ? "RegularError" : "SolidCheckCircle";
                        AnalysisIconBrush = errors > 0
                            ? _themeService?.GetBrush("MainSecondaryColor")
                            : _themeService?.GetBrush("HomeCloudSynchronizationBackGround");
                        
                        break;
                    case LocalInventoryPartStatus.Pending:
                    case LocalInventoryPartStatus.Running:
                        AnalysisIcon = "None";
                        AnalysisIconBrush = _themeService?.GetBrush("HomeCloudSynchronizationBackGround");
                        
                        break;
                    default:
                        AnalysisIcon = "None";
                        AnalysisIconBrush = _themeService?.GetBrush("HomeCloudSynchronizationBackGround");
                        
                        break;
                }
                
                if (status == LocalInventoryPartStatus.Success && errors > 0)
                {
                    AnalysisStatusText =
                        Resources.ResourceManager.GetString("InventoryProcess_AnalysisSuccessWithErrors", Resources.Culture)
                        ?? Resources.InventoryProcess_AnalysisSuccess;
                    
                    // AnalysisIconBrush = _themeService?.GetBrush("HomeCloudSynchronizationBackGround");
                }
                else
                {
                    var key = $"InventoryProcess_Analysis{status}";
                    AnalysisStatusText = Resources.ResourceManager.GetString(key, Resources.Culture) ?? string.Empty;
                    
                    // AnalysisIconBrush = _themeService?.GetBrush("MainSecondaryColor");
                }
                
                // // Brush by status
                // if (status is LocalInventoryPartStatus.Success or LocalInventoryPartStatus.Pending or LocalInventoryPartStatus.Running)
                // {
                //     AnalysisIconBrush = _themeService?.GetBrush("HomeCloudSynchronizationBackGround");
                // }
                // else
                // {
                //     AnalysisIconBrush = _themeService?.GetBrush("MainSecondaryColor");
                // }
            })
            .DisposeWith(disposables);
    }
    
    public extern LocalInventoryPartStatus AnalysisStatus { [ObservableAsProperty] get; }
    
    public extern bool IsAnalysisRunning { [ObservableAsProperty] get; }
    
    public extern bool HasAnalysisStarted { [ObservableAsProperty] get; }
    
    [Reactive]
    public int AnalyzeErrors { get; set; }
    
    public extern bool HasErrors { [ObservableAsProperty] get; }
    
    [Reactive]
    public string AnalysisIcon { get; set; } = "None";
    
    [Reactive]
    public string AnalysisStatusText { get; set; } = string.Empty;
    
    [Reactive]
    public int AnalyzedFiles { get; set; }
    
    [Reactive]
    public long AnalyzableFiles { get; set; }
    
    public long ProcessedSize { get; set; }
    
    [Reactive]
    public IBrush? AnalysisIconBrush { get; set; }
}