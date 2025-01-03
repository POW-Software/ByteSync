﻿using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using DynamicData;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class StatusViewModel : ViewModelBase, IDisposable
{
    private IThemeService _themeService;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;

    private SolidColorBrush? _grayBrush;
    private SolidColorBrush? _mahAppsGray10Brush;
    private SolidColorBrush? _oppositeBackgroundBrush;
    private SolidColorBrush? _mainBackgroundBrush;
    private SolidColorBrush? _mainForeColorBrush;
    private readonly IDisposable _subscription;

    public enum BrushColors
    {
        MahAppsGray10,
        Gray,
        MainForeColor,
        MainBackground,
        OppositeBackground
    }

    public StatusViewModel()
    {
        FingerPrintGroups = new ObservableCollection<StatusItemViewModel>();
        LastWriteTimeGroups = new ObservableCollection<StatusItemViewModel>();
        PresenceGroups = new ObservableCollection<StatusItemViewModel>();

        ShowFileOKStatus = false;
        ShowDirectoryOKStatus = false;
        ShowFileDifferences = false;
        ShowDirectoryDifferences = false;
        ShowSyncSuccessStatus = false;
        ShowSyncErrorStatus = true;
    }

    public StatusViewModel(ComparisonItem comparisonItem, List<Inventory> inventories, IThemeService themeService, 
        ISharedActionsGroupRepository sharedActionsGroupRepository) : this()
    {
        ComparisonItem = comparisonItem;
        AllInventories = inventories;
        Status = comparisonItem.Status!;
        _themeService = themeService;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;

        FingerPrintGroups?.Clear();
        LastWriteTimeGroups?.Clear();
        PresenceGroups?.Clear();
        
        SetUnfinishedStatus();

        var sharedActionsGroups = _sharedActionsGroupRepository.ObservableCache.Connect()
            .Filter(sag => sag.PathIdentity.Equals(Status.PathIdentity))
            .AsObservableCache();
        
        _subscription = sharedActionsGroups.Connect()
            .Throttle(TimeSpan.FromMilliseconds(200)) // Vous pouvez ajuster cela pour minimiser les réactions trop fréquentes
            .Subscribe(_ =>
            {
                var allItems = sharedActionsGroups.Items.ToList();
                if (allItems.All(x => x.SynchronizationStatus == SynchronizationStatus.Success))
                {
                    SetSynchronizationSuccess();
                }
                else if (allItems.All(x => x.SynchronizationStatus == SynchronizationStatus.Error))
                {
                    SetSynchronizationError();
                }
                else
                {
                    if (Status.IsSuccessStatus || Status.IsErrorStatus)
                    {
                        Status.IsSuccessStatus = false;
                        Status.IsErrorStatus = false;
                        
                        SetUnfinishedStatus();
                    }
                }
            });
            

        if (!Status.IsOK)
        {
            using var statusViewGroupsComputer = new StatusViewGroupsComputer(this);
            statusViewGroupsComputer.Compute();

            InitBrushes();
        }
    }

    internal void OnThemeChanged()
    {
        UpdateBrushes();
    }

    private Brush GrayBrush
    {
        get
        {
            if (_grayBrush == null)
            {
                _grayBrush = new SolidColorBrush(Colors.Gray);
            }

            return _grayBrush;
        }
    }

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

    [Reactive]
    public bool ShowFileOKStatus { get; set; }
    
    [Reactive]
    public bool ShowDirectoryOKStatus { get; set; }

    [Reactive]
    public bool ShowFileDifferences { get; set; }
    
    [Reactive]
    public bool ShowDirectoryDifferences { get; set; }

    [Reactive]
    public bool ShowSyncSuccessStatus { get; set; }
    
    [Reactive]
    public bool ShowSyncErrorStatus { get; set; }

    public Status Status { get; }
    
    public ComparisonItem ComparisonItem { get; }
    
    public List<Inventory> AllInventories { get; }

    public ObservableCollection<StatusItemViewModel>? FingerPrintGroups { get; set; }

    public ObservableCollection<StatusItemViewModel>? LastWriteTimeGroups { get; set; }
    
    public ObservableCollection<StatusItemViewModel>? PresenceGroups { get; set; }

    public FileSystemTypes FileSystemType
    {
        get
        {
            return ComparisonItem.FileSystemType;
        }
    }

    private void InitBrushes()
    {
        Dispatcher.UIThread.InvokeAsync(() => { DoResetBrushes(true); });
    }
    
    private void UpdateBrushes()
    {
        Dispatcher.UIThread.InvokeAsync(() => { DoResetBrushes(false); });
    }

    private void DoResetBrushes(bool isInit)
    {
        // Reset des brushs dynamiques
        _mainForeColorBrush = null;
        // _mainBackColorBrush = null;
        _oppositeBackgroundBrush = null;
        _mainBackgroundBrush = null;
        _mahAppsGray10Brush = null;

        if (FingerPrintGroups != null)
        {
            foreach (var statusItemViewModel in FingerPrintGroups)
            {
                DoResetBrushes(isInit, statusItemViewModel);
            }
        }

        if (LastWriteTimeGroups != null)
        {
            foreach (var statusItemViewModel in LastWriteTimeGroups)
            {
                DoResetBrushes(isInit, statusItemViewModel);
            }
        }
        
        if (PresenceGroups != null)
        {
            foreach (var statusItemViewModel in PresenceGroups)
            {
                DoResetBrushes(isInit, statusItemViewModel);
            }
        }
    }

    private void DoResetBrushes(bool isInit, StatusItemViewModel statusItemViewModel)
    {
        if (isInit)
        {
            statusItemViewModel.InitBrushes(GetBrush(statusItemViewModel.ForeBrushColor), GetBrush(statusItemViewModel.BackBrushColor));
        }
        else
        {
            statusItemViewModel.BackBrush = GetBrush(statusItemViewModel.BackBrushColor);
            statusItemViewModel.ForeBrush = GetBrush(statusItemViewModel.ForeBrushColor);
        }
    }

    private Brush? GetBrush(BrushColors brushColor)
    {
        switch (brushColor)
        {
            case BrushColors.Gray:
                return GrayBrush;

            case BrushColors.MahAppsGray10:
                return MahAppsGray10Brush;

            case BrushColors.MainBackground:
                return MainBackgroundBrush;
                
            case BrushColors.OppositeBackground:
                return OppositeBackgroundBrush;

            case BrushColors.MainForeColor:
                return MainForeColorBrush;
        }

        return null;
    }

    public void SetSynchronizationSuccess()
    {
        ShowFileOKStatus = false;
        ShowDirectoryOKStatus = false;
        ShowFileDifferences = false;
        ShowDirectoryDifferences = false;
        ShowSyncSuccessStatus = true;
        ShowSyncErrorStatus = false;

        Status.IsSuccessStatus = true;
    }
    
    public void SetSynchronizationError()
    {
        ShowFileOKStatus = false;
        ShowDirectoryOKStatus = false;
        ShowFileDifferences = false;
        ShowDirectoryDifferences = false;
        ShowSyncSuccessStatus = false;
        ShowSyncErrorStatus = true;
        
        Status.IsErrorStatus = true;
    }
    
    private void SetUnfinishedStatus()
    {
        ShowFileOKStatus = Status.IsOK && FileSystemType == FileSystemTypes.File;
        ShowDirectoryOKStatus = Status.IsOK && FileSystemType == FileSystemTypes.Directory;
        ShowFileDifferences = !Status.IsOK && FileSystemType == FileSystemTypes.File;
        ShowDirectoryDifferences = !Status.IsOK && FileSystemType == FileSystemTypes.Directory;
        
        ShowSyncSuccessStatus = Status.IsSuccessStatus;
        ShowSyncErrorStatus = Status.IsErrorStatus;
    }

    public void Dispose()
    {
        FingerPrintGroups = null;
        LastWriteTimeGroups = null;
        PresenceGroups = null;
        
        _subscription.Dispose();
    }
}