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

public class InventoryLocalIdentificationViewModel : ActivatableViewModelBase
{
    private readonly IInventoryService _inventoryService = null!;
    private readonly IThemeService _themeService = null!;
    
    public InventoryLocalIdentificationViewModel()
    {
    }
    
    public InventoryLocalIdentificationViewModel(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
        
        this.WhenActivated(HandleActivation);
    }
    
    public InventoryLocalIdentificationViewModel(IInventoryService inventoryService, IThemeService themeService)
        : this(inventoryService)
    {
        _themeService = themeService;
        IdentificationIconBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
    }
    
    private void HandleActivation(CompositeDisposable disposables)
    {
        _inventoryService.InventoryProcessData.IdentificationStatus
            .ToPropertyEx(this, x => x.IdentificationStatus)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.IdentificationStatus
            .Select(ms => ms == InventoryTaskStatus.Running)
            .ToPropertyEx(this, x => x.IsIdentificationRunning)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.InventoryMonitorObservable
            .Sample(TimeSpan.FromMilliseconds(500))
            .Subscribe(m =>
            {
                IdentifiedFiles = m.IdentifiedFiles;
                IdentifiedDirectories = m.IdentifiedDirectories;
                IdentifiedVolume = m.IdentifiedVolume;
                IdentificationErrors = m.IdentificationErrors;
                SkippedEntriesCount = m.SkippedEntriesCount;
            })
            .DisposeWith(disposables);
        
        this.WhenAnyValue(x => x.IdentificationErrors)
            .Select(v => v > 0)
            .ToPropertyEx(this, x => x.HasIdentificationErrors)
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.SkippedEntriesCount)
            .Select(v => v > 0)
            .ToPropertyEx(this, x => x.ShowSkippedEntriesCount)
            .DisposeWith(disposables);
        
        _inventoryService.InventoryProcessData.IdentificationStatus
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(status =>
            {
                switch (status)
                {
                    case InventoryTaskStatus.Error:
                    case InventoryTaskStatus.Cancelled:
                    case InventoryTaskStatus.NotLaunched:
                        IdentificationIcon = "SolidXCircle";
                        SetIdentificationBrush(status);
                        
                        break;
                    case InventoryTaskStatus.Success:
                        IdentificationIcon = "SolidCheckCircle";
                        SetIdentificationBrush(status);
                        
                        break;
                    case InventoryTaskStatus.Pending:
                        IdentificationIcon = "RegularPauseCircle";
                        SetIdentificationBrush(status);
                        
                        break;
                    case InventoryTaskStatus.Running:
                        IdentificationIcon = "None";
                        SetIdentificationBrush(status);
                        
                        break;
                    default:
                        IdentificationIcon = "None";
                        SetIdentificationBrush(status);
                        
                        break;
                }
                
                string key = status switch
                {
                    InventoryTaskStatus.Success => "InventoryProcess_IdentificationSuccess",
                    InventoryTaskStatus.Cancelled => "InventoryProcess_IdentificationCancelled",
                    InventoryTaskStatus.Error => "InventoryProcess_IdentificationError",
                    InventoryTaskStatus.Pending => "InventoryProcess_IdentificationRunning",
                    InventoryTaskStatus.Running => "InventoryProcess_IdentificationRunning",
                    InventoryTaskStatus.NotLaunched => "InventoryProcess_IdentificationCancelled",
                    _ => string.Empty
                };
                IdentificationStatusText = string.IsNullOrEmpty(key)
                    ? string.Empty
                    : Resources.ResourceManager.GetString(key, Resources.Culture) ?? string.Empty;
            })
            .DisposeWith(disposables);
        
        _themeService.SelectedTheme
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => { SetIdentificationBrush(IdentificationStatus); })
            .DisposeWith(disposables);
    }
    
    public extern InventoryTaskStatus IdentificationStatus { [ObservableAsProperty] get; }
    
    public extern bool IsIdentificationRunning { [ObservableAsProperty] get; }
    
    [Reactive]
    public int IdentifiedFiles { get; set; }
    
    [Reactive]
    public int IdentifiedDirectories { get; set; }
    
    [Reactive]
    public long IdentifiedVolume { get; set; }
    
    [Reactive]
    public int IdentificationErrors { get; set; }

    public extern bool HasIdentificationErrors { [ObservableAsProperty] get; }

    [Reactive]
    public int SkippedEntriesCount { get; set; }

    public extern bool ShowSkippedEntriesCount { [ObservableAsProperty] get; }
    
    [Reactive]
    public string IdentificationIcon { get; set; } = "None";
    
    [Reactive]
    public string IdentificationStatusText { get; set; } = string.Empty;
    
    [Reactive]
    public IBrush? IdentificationIconBrush { get; set; }
    
    private void SetIdentificationBrush(InventoryTaskStatus status)
    {
        switch (status)
        {
            case InventoryTaskStatus.Error:
            case InventoryTaskStatus.Cancelled:
            case InventoryTaskStatus.NotLaunched:
                IdentificationIconBrush = _theme_service_get_secondary();
                
                break;
            case InventoryTaskStatus.Success:
                IdentificationIconBrush = _theme_service_get_background();
                
                break;
            case InventoryTaskStatus.Pending:
            case InventoryTaskStatus.Running:
            default:
                IdentificationIconBrush = _theme_service_get_background();
                
                break;
        }
    }
    
    private IBrush _theme_service_get_background() => _themeService.GetBrush("HomeCloudSynchronizationBackGround");
    private IBrush _theme_service_get_secondary() => _themeService.GetBrush("MainSecondaryColor");
}
