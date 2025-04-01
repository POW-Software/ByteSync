using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.Themes;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class ItemSynchronizationStatusViewModel : ViewModelBase, IDisposable
{
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    private readonly IThemeService _themeService;
    
    private readonly CompositeDisposable _disposables = new();
    
    private SolidColorBrush? _mahAppsGray10Brush;
    private SolidColorBrush? _oppositeBackgroundBrush;
    private SolidColorBrush? _mainBackgroundBrush;
    private SolidColorBrush? _mainForeColorBrush;

    public ItemSynchronizationStatusViewModel()
    {

    }

    public ItemSynchronizationStatusViewModel(ComparisonItem comparisonItem, IThemeService themeService, 
        ISharedActionsGroupRepository sharedActionsGroupRepository) : this()
    {
        ComparisonItem = comparisonItem;
        ItemSynchronizationStatus = comparisonItem.ItemSynchronizationStatus!;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _themeService = themeService;
        
        InitializeStatuses();

        var sharedActionsGroups = _sharedActionsGroupRepository.ObservableCache.Connect()
            .Filter(sag => sag.PathIdentity.Equals(ItemSynchronizationStatus.PathIdentity))
            .AsObservableCache();
        
        var subscription = sharedActionsGroups.Connect()
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(_ =>
            {
                var allItems = sharedActionsGroups.Items.ToList();
                if (allItems.All(x => x.SynchronizationStatus == Business.Actions.Shared.SynchronizationStatus.Success))
                {
                    SetSynchronizationSuccess();
                }
                else if (allItems.All(x => x.SynchronizationStatus == Business.Actions.Shared.SynchronizationStatus.Error))
                {
                    SetSynchronizationError();
                }
                else
                {
                    if (ItemSynchronizationStatus.IsSuccessStatus || ItemSynchronizationStatus.IsErrorStatus)
                    {
                        ItemSynchronizationStatus.IsSuccessStatus = false;
                        ItemSynchronizationStatus.IsErrorStatus = false;
                        
                        SetUnfinishedStatus();
                    }
                }
            });
        
        _disposables.Add(subscription);
        
        subscription = _themeService.SelectedTheme
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnThemeChanged);
        
        _disposables.Add(subscription);
    }
    
    public ItemSynchronizationStatus ItemSynchronizationStatus { get; }
    
    public ComparisonItem ComparisonItem { get; }
    
    [Reactive]
    public bool ShowSyncSuccessStatus { get; set; }
    
    [Reactive]
    public bool ShowSyncErrorStatus { get; set; }
    
    public Brush? MainForeColorBrush
    {
        get
        {
            if (_mainForeColorBrush == null)
            {
                _themeService.GetResource("SystemControlForegroundBaseHighBrush", out _mainForeColorBrush);
            }

            return _mainForeColorBrush;
        }
    }

    public Brush? MahAppsGray10Brush
    {
        get
        {
            if (_mahAppsGray10Brush == null)
            {
                _themeService.GetResource("VeryLightGrayBrush", out _mahAppsGray10Brush);
            }

            return _mahAppsGray10Brush;
        }
    }
    
    private Brush? MainBackgroundBrush
    {
        get
        {
            if (_mainBackgroundBrush == null)
            {
                _themeService.GetResource("StatusMainBackGroundBrush", out _mainBackgroundBrush);
            }
    
            return _mainBackgroundBrush;
        }
    }
    
    private Brush? OppositeBackgroundBrush
    {
        get
        {
            if (_oppositeBackgroundBrush == null)
            {
                _themeService.GetResource("StatusOppositeBackGroundBrush", out _oppositeBackgroundBrush);
            }

            return _oppositeBackgroundBrush;
        }
    }
    
    private void OnThemeChanged(Theme theme)
    {
        _mainForeColorBrush = null;
        _oppositeBackgroundBrush = null;
        _mainBackgroundBrush = null;
        _mahAppsGray10Brush = null;
    }
    
    public void SetSynchronizationSuccess()
    {
        ItemSynchronizationStatus.IsSuccessStatus = true;
        ShowSyncSuccessStatus = true;
    }
    
    public void SetSynchronizationError()
    {
        ItemSynchronizationStatus.IsErrorStatus = true;
        ShowSyncErrorStatus = true;
    }
    
    private void InitializeStatuses()
    {
        ItemSynchronizationStatus.IsSuccessStatus = ItemSynchronizationStatus.IsSuccessStatus;
        ItemSynchronizationStatus.IsErrorStatus =  ItemSynchronizationStatus.IsErrorStatus;
    }
    
    private void SetUnfinishedStatus()
    {
        ItemSynchronizationStatus.IsSuccessStatus = false;
        ShowSyncSuccessStatus = false;
        
        ItemSynchronizationStatus.IsErrorStatus =  ItemSynchronizationStatus.IsErrorStatus;
        ShowSyncErrorStatus = false;
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}