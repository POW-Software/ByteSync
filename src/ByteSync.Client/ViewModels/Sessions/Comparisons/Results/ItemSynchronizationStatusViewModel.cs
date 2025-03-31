using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using DynamicData;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class ItemSynchronizationStatusViewModel : ViewModelBase, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    private readonly IThemeService _themeService;
    
    private SolidColorBrush? _grayBrush;
    private SolidColorBrush? _mahAppsGray10Brush;
    private SolidColorBrush? _oppositeBackgroundBrush;
    private SolidColorBrush? _mainBackgroundBrush;
    private SolidColorBrush? _mainForeColorBrush;

    public ItemSynchronizationStatusViewModel()
    {
        // FingerPrintGroups = new ObservableCollection<StatusItemViewModel>();
        // LastWriteTimeGroups = new ObservableCollection<StatusItemViewModel>();
        // PresenceGroups = new ObservableCollection<StatusItemViewModel>();
        //
        // ShowFileOKStatus = false;
        // ShowDirectoryOKStatus = false;
        // ShowFileDifferences = false;
        // ShowDirectoryDifferences = false;
        // ShowSyncSuccessStatus = false;
        // ShowSyncErrorStatus = true;
    }

    public ItemSynchronizationStatusViewModel(ComparisonItem comparisonItem, List<Inventory> inventories, IThemeService themeService, 
        ISharedActionsGroupRepository sharedActionsGroupRepository, IContentRepartitionGroupsComputerFactory contentRepartitionGroupsComputerFactory ) : this()
    {
        ComparisonItem = comparisonItem;
        // AllInventories = inventories;
        ItemSynchronizationStatus = comparisonItem.ItemSynchronizationStatus!;
        // _themeService = themeService;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _themeService = themeService;

        // FingerPrintGroups?.Clear();
        // LastWriteTimeGroups?.Clear();
        // PresenceGroups?.Clear();
        
        SetUnfinishedStatus();

        var sharedActionsGroups = _sharedActionsGroupRepository.ObservableCache.Connect()
            .Filter(sag => sag.PathIdentity.Equals(ItemSynchronizationStatus.PathIdentity))
            .AsObservableCache();
        
        _subscription = sharedActionsGroups.Connect()
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
            

        // if (!ContentRepartition.IsOK)
        // {
        //     var statusViewGroupsComputer = contentRepartitionGroupsComputerFactory.BuildStatusViewGroupsComputer(this);
        //     statusViewGroupsComputer.Compute();
        //
        //     InitBrushes();
        // }
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
    
    public void SetSynchronizationSuccess()
    {
        // ShowFileOKStatus = false;
        // ShowDirectoryOKStatus = false;
        // ShowFileDifferences = false;
        // ShowDirectoryDifferences = false;
        // ShowSyncSuccessStatus = true;
        // ShowSyncErrorStatus = false;

        ItemSynchronizationStatus.IsSuccessStatus = true;
        ShowSyncSuccessStatus = true;
    }
    
    public void SetSynchronizationError()
    {
        // ShowFileOKStatus = false;
        // ShowDirectoryOKStatus = false;
        // ShowFileDifferences = false;
        // ShowDirectoryDifferences = false;
        // ShowSyncSuccessStatus = false;
        // ShowSyncErrorStatus = true;
        
        ItemSynchronizationStatus.IsErrorStatus = true;
        ShowSyncErrorStatus = true;
    }
    
    private void SetUnfinishedStatus()
    {
        // ShowFileOKStatus = ContentRepartition.IsOK && FileSystemType == FileSystemTypes.File;
        // ShowDirectoryOKStatus = ContentRepartition.IsOK && FileSystemType == FileSystemTypes.Directory;
        // ShowFileDifferences = !ContentRepartition.IsOK && FileSystemType == FileSystemTypes.File;
        // ShowDirectoryDifferences = !ContentRepartition.IsOK && FileSystemType == FileSystemTypes.Directory;
        //
        // ShowSyncSuccessStatus = ContentRepartition.IsSuccessStatus;
        // ShowSyncErrorStatus = ContentRepartition.IsErrorStatus;
        
        ItemSynchronizationStatus.IsSuccessStatus = false;
        ItemSynchronizationStatus.IsErrorStatus = false;
    }
    
    public void Dispose()
    {
        // FingerPrintGroups = null;
        // LastWriteTimeGroups = null;
        // PresenceGroups = null;
        
        _subscription.Dispose();
    }
}