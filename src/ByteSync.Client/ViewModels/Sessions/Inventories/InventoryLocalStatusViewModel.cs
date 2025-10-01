using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Inventories;

public class InventoryLocalStatusViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService = null!;
    private readonly ITimeTrackingCache _timeTrackingCache = null!;
    private readonly IInventoryService _inventoryService = null!;
    private readonly IThemeService? _themeService;
    private InventoryTaskStatus _currentLocalStatus;
    
    public InventoryLocalStatusViewModel()
    {
        EstimatedProcessEndName = Resources.InventoryProcess_EstimatedEnd;
        LocalMainIcon = "None";
        LocalMainStatusText = string.Empty;
        LocalMainIconBrush = null;
    }
    
    public InventoryLocalStatusViewModel(ISessionService sessionService, ITimeTrackingCache timeTrackingCache,
        IInventoryService inventoryService) : this(sessionService, timeTrackingCache, inventoryService, null)
    {
    }
    
    public InventoryLocalStatusViewModel(ISessionService sessionService, ITimeTrackingCache timeTrackingCache,
        IInventoryService inventoryService, IThemeService? themeService) : this()
    {
        _sessionService = sessionService;
        _timeTrackingCache = timeTrackingCache;
        _inventoryService = inventoryService;
        _themeService = themeService;
        
        this.WhenActivated(disposables =>
        {
            var timeTrackingComputer = _timeTrackingCache
                .GetTimeTrackingComputer(_sessionService.SessionId!, TimeTrackingComputerType.Inventory)
                .Result;
            timeTrackingComputer.RemainingTime
                .Subscribe(remainingTime =>
                {
                    RemainingTime = remainingTime.RemainingTime;
                    ElapsedTime = remainingTime.ElapsedTime;
                    EstimatedEndDateTime = remainingTime.EstimatedEndDateTime;
                    StartDateTime = remainingTime.StartDateTime;
                })
                .DisposeWith(disposables);
            
            _inventoryService.InventoryProcessData.GlobalMainStatus
                .Select(s => s is InventoryTaskStatus.Pending or InventoryTaskStatus.Running
                    ? Resources.InventoryProcess_EstimatedEnd
                    : Resources.InventoryProcess_End)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(text => EstimatedProcessEndName = text)
                .DisposeWith(disposables);
            
            var statusStream = _inventoryService.InventoryProcessData.MainStatus
                .DistinctUntilChanged()
                .Replay(1)
                .RefCount();
            
            statusStream
                .Select(st => st is InventoryTaskStatus.Pending or InventoryTaskStatus.Running)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsLocalInventoryInProgress)
                .DisposeWith(disposables);
            
            statusStream
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(st =>
                {
                    _currentLocalStatus = st;
                    switch (st)
                    {
                        case InventoryTaskStatus.Pending:
                        case InventoryTaskStatus.Running:
                            LocalMainIcon = "None";
                            LocalMainStatusText =
                                Resources.ResourceManager.GetString("InventoryProcess_LocalInventory_Running", Resources.Culture)
                                ?? Resources.InventoryProcess_InventoryRunning;
                            LocalMainIconBrush = null;
                            
                            break;
                        case InventoryTaskStatus.Success:
                            LocalMainIcon = "SolidCheckCircle";
                            LocalMainStatusText =
                                Resources.ResourceManager.GetString("InventoryProcess_LocalInventory_Success", Resources.Culture)
                                ?? Resources.InventoryProcess_InventorySuccess;
                            LocalMainIconBrush = _themeService?.GetBrush("HomeCloudSynchronizationBackGround");
                            
                            break;
                        case InventoryTaskStatus.Cancelled:
                            LocalMainIcon = "SolidXCircle";
                            LocalMainStatusText =
                                Resources.ResourceManager.GetString("InventoryProcess_LocalInventory_Cancelled", Resources.Culture)
                                ?? Resources.InventoryProcess_InventoryCancelled;
                            LocalMainIconBrush = _themeService?.GetBrush("MainSecondaryColor");
                            
                            break;
                        case InventoryTaskStatus.Error:
                            LocalMainIcon = "SolidXCircle";
                            LocalMainStatusText =
                                Resources.ResourceManager.GetString("InventoryProcess_LocalInventory_Error", Resources.Culture)
                                ?? Resources.InventoryProcess_InventoryError;
                            LocalMainIconBrush = _themeService?.GetBrush("MainSecondaryColor");
                            
                            break;
                        case InventoryTaskStatus.NotLaunched:
                        default:
                            LocalMainIcon = "SolidXCircle";
                            LocalMainStatusText =
                                Resources.ResourceManager.GetString("InventoryProcess_LocalInventory_NotLaunched", Resources.Culture)
                                ?? Resources.InventoryProcess_InventoryError;
                            LocalMainIconBrush = _themeService?.GetBrush("MainSecondaryColor");
                            
                            break;
                    }
                })
                .DisposeWith(disposables);
            
            _themeService?.SelectedTheme
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    switch (_currentLocalStatus)
                    {
                        case InventoryTaskStatus.Success:
                            LocalMainIconBrush = _themeService?.GetBrush("HomeCloudSynchronizationBackGround");
                            
                            break;
                        case InventoryTaskStatus.Pending:
                        case InventoryTaskStatus.Running:
                            LocalMainIconBrush = null;
                            
                            break;
                        case InventoryTaskStatus.Error:
                        case InventoryTaskStatus.Cancelled:
                        case InventoryTaskStatus.NotLaunched:
                        default:
                            LocalMainIconBrush = _themeService?.GetBrush("MainSecondaryColor");
                            
                            break;
                    }
                })
                .DisposeWith(disposables);
        });
    }
    
    [Reactive]
    public string EstimatedProcessEndName { get; set; }
    
    [Reactive]
    public DateTime? StartDateTime { get; set; }
    
    [Reactive]
    public TimeSpan ElapsedTime { get; set; }
    
    [Reactive]
    public DateTime? EstimatedEndDateTime { get; set; }
    
    [Reactive]
    public TimeSpan? RemainingTime { get; set; }
    
    public extern bool IsLocalInventoryInProgress { [ObservableAsProperty] get; }
    
    [Reactive]
    public string LocalMainIcon { get; set; }
    
    [Reactive]
    public string LocalMainStatusText { get; set; }
    
    [Reactive]
    public IBrush? LocalMainIconBrush { get; set; }
}