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
    private IThemeService _themeService = null!;

    private IBrush? _grayBrush;
    private IBrush? _lightGrayBrush;
    private IBrush? _secondaryBackgroundBrush;
    private IBrush? _mainBackgroundBrush;
    private IBrush? _mainForeColorBrush;
    private readonly IDisposable _subscription = null!;

    public enum BrushColors
    {
        LightGray,
        Gray,
        MainForeColor,
        MainBackground,
        SecondaryBackground
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

    private ContentRepartitionComputeResult ComputeResult { get; set; } = null!;

    private IBrush GrayBrush
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

    public IBrush? MainForeColorBrush
    {
        get
        {
            if (_mainForeColorBrush == null)
            {
                _mainForeColorBrush = _themeService.GetBrush("SystemControlForegroundBaseHighBrush");
            }

            return _mainForeColorBrush;
        }
    }

    public IBrush? LightGrayBrush
    {
        get
        {
            if (_lightGrayBrush == null)
            {
                _lightGrayBrush = _themeService.GetBrush("VeryLightGrayBrush");
            }

            return _lightGrayBrush;
        }
    }
    
    private IBrush? MainBackgroundBrush
    {
        get
        {
            if (_mainBackgroundBrush == null)
            {
                _mainBackgroundBrush = _themeService.GetBrush("StatusMainBackGroundBrush");
            }

            return _mainBackgroundBrush;
        }
    }

    private IBrush? SecondaryBackgroundBrush
    {
        get
        {
            if (_secondaryBackgroundBrush == null)
            {
                _secondaryBackgroundBrush = _themeService.GetBrush("StatusSecondaryBackGroundBrush");
            }

            return _secondaryBackgroundBrush;
        }
    }

    [Reactive]
    public bool ShowFileDifferences { get; set; }
    
    [Reactive]
    public bool ShowDirectoryDifferences { get; set; }

    public ContentRepartition ContentRepartition { get; } = null!;

    public ComparisonItem ComparisonItem { get; } = null!;

    public List<Inventory> AllInventories { get; } = null!;

    public ObservableCollection<StatusItemViewModel>? FingerPrintGroups { get; set; }

    public ObservableCollection<StatusItemViewModel>? LastWriteTimeGroups { get; set; }
    
    public ObservableCollection<StatusItemViewModel>? PresenceGroups { get; set; }
    
    [Reactive]
    public IBrush? HashBackBrush { get; set; }
    
    [Reactive]
    public IBrush? TimeBackBrush { get; set; }
    
    [Reactive]
    public IBrush? FolderBackBrush { get; set; }

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
        _secondaryBackgroundBrush = null;
        _mainBackgroundBrush = null;
        _lightGrayBrush = null;

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
            HashBackBrush = LightGrayBrush;
        }

        if (ComputeResult.LastWriteTimeGroups == 1)
        {
            TimeBackBrush = MainBackgroundBrush;
        }
        else
        {
            TimeBackBrush = LightGrayBrush;
        }
        
        if (ComputeResult.PresenceGroups == 1)
        {
            FolderBackBrush = MainBackgroundBrush;
        }
        else
        {
            FolderBackBrush = LightGrayBrush;
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

    private IBrush? GetBrush(BrushColors brushColor)
    {
        switch (brushColor)
        {
            case BrushColors.Gray:
                return GrayBrush;

            case BrushColors.LightGray:
                return LightGrayBrush;

            case BrushColors.MainBackground:
                return MainBackgroundBrush;
                
            case BrushColors.SecondaryBackground:
                return SecondaryBackgroundBrush;

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