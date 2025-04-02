using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Themes;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Factories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class ContentRepartitionViewModel : ViewModelBase, IDisposable
{
    private IThemeService _themeService;

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

    public ContentRepartitionViewModel()
    {
        FingerPrintGroups = new ObservableCollection<StatusItemViewModel>();
        LastWriteTimeGroups = new ObservableCollection<StatusItemViewModel>();
        PresenceGroups = new ObservableCollection<StatusItemViewModel>();
        
        ShowFileDifferences = false;
        ShowDirectoryDifferences = false;
    }

    public ContentRepartitionViewModel(ComparisonItem comparisonItem, List<Inventory> inventories, IThemeService themeService, 
        IContentRepartitionGroupsComputerFactory contentRepartitionGroupsComputerFactory ) : this()
    {
        ComparisonItem = comparisonItem;
        AllInventories = inventories;
        ContentRepartition = comparisonItem.ContentRepartition!;
        _themeService = themeService;

        FingerPrintGroups?.Clear();
        LastWriteTimeGroups?.Clear();
        PresenceGroups?.Clear();
        
        InitializeMode();
        
        var contentRepartitionGroupsComputer = contentRepartitionGroupsComputerFactory.Build(this);
        ComputeResult = contentRepartitionGroupsComputer.Compute();

        _subscription = _themeService.SelectedTheme
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnThemeChanged);

        InitBrushes();
    }

    private ContentRepartitionComputeResult ComputeResult { get; set; }

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
    public bool ShowFileDifferences { get; set; }
    
    [Reactive]
    public bool ShowDirectoryDifferences { get; set; }

    public ContentRepartition ContentRepartition { get; }
    
    public ComparisonItem ComparisonItem { get; }
    
    public List<Inventory> AllInventories { get; }

    public ObservableCollection<StatusItemViewModel>? FingerPrintGroups { get; set; }

    public ObservableCollection<StatusItemViewModel>? LastWriteTimeGroups { get; set; }
    
    public ObservableCollection<StatusItemViewModel>? PresenceGroups { get; set; }
    
    [Reactive]
    public Brush? HashBackBrush { get; set; }
    
    [Reactive]
    public Brush? TimeBackBrush { get; set; }
    
    [Reactive]
    public Brush? FolderBackBrush { get; set; }

    public FileSystemTypes FileSystemType
    {
        get
        {
            return ComparisonItem.FileSystemType;
        }
    }

    private void InitBrushes()
    {
        DoResetBrushes(true);
    }
    
    private void UpdateBrushes()
    {
        DoResetBrushes(false);
    }

    private void DoResetBrushes(bool isInit)
    {
        _mainForeColorBrush = null;
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

        UpdateBackBrushes();
    }

    private void UpdateBackBrushes()
    {
        if (ComputeResult.FingerPrintGroups == 1)
        {
            HashBackBrush = MainBackgroundBrush;
        }
        else
        {
            HashBackBrush = MahAppsGray10Brush;
        }

        if (ComputeResult.LastWriteTimeGroups == 1)
        {
            TimeBackBrush = MainBackgroundBrush;
        }
        else
        {
            TimeBackBrush = MahAppsGray10Brush;
        }
        
        if (ComputeResult.PresenceGroups == 1)
        {
            FolderBackBrush = MainBackgroundBrush;
        }
        else
        {
            FolderBackBrush = MahAppsGray10Brush;
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
    
    private void InitializeMode()
    {
        ShowFileDifferences = FileSystemType == FileSystemTypes.File;
        ShowDirectoryDifferences = FileSystemType == FileSystemTypes.Directory;
    }
    
    private void OnThemeChanged(Theme theme)
    {
        UpdateBrushes();
    }

    public void Dispose()
    {
        FingerPrintGroups = null;
        LastWriteTimeGroups = null;
        PresenceGroups = null;
        
        _subscription.Dispose();
    }
}