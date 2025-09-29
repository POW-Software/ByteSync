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

public class InventoryDeltaGenerationViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService = null!;
    private readonly IThemeService _themeService = null!;
    
    public InventoryDeltaGenerationViewModel()
    {
    }
    
    public InventoryDeltaGenerationViewModel(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
        
        this.WhenActivated(HandleActivation);
    }
    
    public InventoryDeltaGenerationViewModel(IInventoryService inventoryService, IThemeService themeService)
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
            .Select(ms => ms == InventoryTaskStatus.Running)
            .ToPropertyEx(this, x => x.IsAnalysisRunning)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.AnalysisStatus
            .Select(ms => ms == InventoryTaskStatus.Running && !HasAnalysisStarted)
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
        
        // AnalyzeSuccess = AnalyzedFiles - AnalyzeErrors
        this.WhenAnyValue(x => x.AnalyzedFiles, x => x.AnalyzeErrors)
            .Select(x => x.Item1 - x.Item2)
            .ToPropertyEx(this, x => x.AnalyzeSuccess)
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
                    case InventoryTaskStatus.Error:
                    case InventoryTaskStatus.Cancelled:
                    case InventoryTaskStatus.NotLaunched:
                        AnalysisIcon = "SolidXCircle";
                        AnalysisIconBrush = _themeService.GetBrush("MainSecondaryColor");
                        
                        break;
                    case InventoryTaskStatus.Success:
                        AnalysisIcon = errors > 0 ? "RegularError" : "SolidCheckCircle";
                        AnalysisIconBrush = errors > 0
                            ? _themeService.GetBrush("MainSecondaryColor")
                            : _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                        
                        break;
                    case InventoryTaskStatus.Pending:
                        AnalysisIcon = "RegularPauseCircle";
                        AnalysisIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                        
                        break;
                    case InventoryTaskStatus.Running:
                        AnalysisIcon = "None";
                        AnalysisIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                        
                        break;
                    default:
                        AnalysisIcon = "None";
                        AnalysisIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                        
                        break;
                }
                
                if (status == InventoryTaskStatus.Success && errors > 0)
                {
                    AnalysisStatusText =
                        Resources.ResourceManager.GetString("InventoryProcess_DeltaGeneration_SuccessWithErrors", Resources.Culture)
                        ?? Resources.InventoryProcess_DeltaGeneration_Success;
                }
                else
                {
                    var key = $"InventoryProcess_DeltaGeneration_{status}";
                    AnalysisStatusText = Resources.ResourceManager.GetString(key, Resources.Culture) ?? string.Empty;
                }
            })
            .DisposeWith(disposables);
    }
    
    public extern InventoryTaskStatus AnalysisStatus { [ObservableAsProperty] get; }
    
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
    
    public extern int AnalyzeSuccess { [ObservableAsProperty] get; }
    
    [Reactive]
    public long ProcessedSize { get; set; }
    
    [Reactive]
    public IBrush? AnalysisIconBrush { get; set; }
}