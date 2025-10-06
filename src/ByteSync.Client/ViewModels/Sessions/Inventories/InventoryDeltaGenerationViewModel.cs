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
            .Select(ms => ms != InventoryTaskStatus.Pending && ms != InventoryTaskStatus.NotLaunched)
            .StartWith(false)
            .DistinctUntilChanged()
            .ToPropertyEx(this, x => x.ShowAnalysisValues)
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
        
        this.WhenAnyValue(x => x.AnalyzedFiles, x => x.AnalyzableFiles)
            .Select(t => t.Item2 > 0 ? t.Item1 / (double)t.Item2 : 0d)
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.Progress)
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
                        AnalysisIcon = "SolidXCircle";
                        SetAnalysisBrush(status, errors);
                        
                        break;
                    
                    case InventoryTaskStatus.NotLaunched:
                        AnalysisIcon = "SolidMinusCircle";
                        SetAnalysisBrush(status, errors);
                        
                        break;
                    case InventoryTaskStatus.Success:
                        AnalysisIcon = errors > 0 ? "RegularError" : "SolidCheckCircle";
                        SetAnalysisBrush(status, errors);
                        
                        break;
                    case InventoryTaskStatus.Pending:
                        AnalysisIcon = "RegularPauseCircle";
                        SetAnalysisBrush(status, errors);
                        
                        break;
                    case InventoryTaskStatus.Running:
                    default:
                        AnalysisIcon = "None";
                        SetAnalysisBrush(status, errors);
                        
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
        
        // Update the icon brush when the theme changes to keep in sync
        _themeService.SelectedTheme
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => { SetAnalysisBrush(AnalysisStatus, AnalyzeErrors); })
            .DisposeWith(disposables);
    }
    
    public extern InventoryTaskStatus AnalysisStatus { [ObservableAsProperty] get; }
    
    public extern bool IsAnalysisRunning { [ObservableAsProperty] get; }
    
    public extern bool ShowAnalysisValues { [ObservableAsProperty] get; }
    
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
    
    public extern double Progress { [ObservableAsProperty] get; }
    
    private void SetAnalysisBrush(InventoryTaskStatus status, int errors)
    {
        switch (status)
        {
            case InventoryTaskStatus.Error:
            case InventoryTaskStatus.Cancelled:
            case InventoryTaskStatus.NotLaunched:
                AnalysisIconBrush = _themeService.GetBrush("MainSecondaryColor");
                
                break;
            case InventoryTaskStatus.Success:
                AnalysisIconBrush = errors > 0
                    ? _themeService.GetBrush("MainSecondaryColor")
                    : _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                
                break;
            case InventoryTaskStatus.Pending:
            case InventoryTaskStatus.Running:
            default:
                AnalysisIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
                
                break;
        }
    }
}