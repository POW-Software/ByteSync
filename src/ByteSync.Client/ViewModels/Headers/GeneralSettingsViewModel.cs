﻿using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using Avalonia.Controls.Mixins;
using ByteSync.Business.Configurations;
using ByteSync.Common.Interfaces;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Updates;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Headers;

public class GeneralSettingsViewModel : FlyoutElementViewModel
{
    private readonly ILocalApplicationDataManager _localApplicationDataManager;
    private readonly IWebAccessor _webAccessor;
    private readonly IFileSystemAccessor _fileSystemAccessor;
    private readonly IThemeService _themeService;
    private readonly IZoomService _zoomService;
    private readonly IApplicationRestarter _applicationRestarter;
    private readonly ILogger<GeneralSettingsViewModel> _logger;

    public GeneralSettingsViewModel()
    {
    }

    public GeneralSettingsViewModel(ILocalApplicationDataManager localApplicationDataManager, IWebAccessor webAccessor, 
        IFileSystemAccessor fileSystemAccessor, IThemeService themeManager, IZoomService zoomService,
        IApplicationRestarter applicationRestarter, ILogger<GeneralSettingsViewModel> logger)
    {
        _localApplicationDataManager = localApplicationDataManager;
        _webAccessor = webAccessor;
        _fileSystemAccessor = fileSystemAccessor;
        _themeService = themeManager;
        _zoomService = zoomService;
        _applicationRestarter = applicationRestarter;
        _logger = logger;

        Locale = Services.ContainerProvider.Container.Resolve<SelectLocaleViewModel>();

        VisitPowSoftwareCommand = ReactiveCommand.CreateFromTask(VisitPowSoftware);
        VisitPowSoftwareCommand.ThrownExceptions.Subscribe(OnCommmandException);
            
        ExploreAppDataCommand = ReactiveCommand.CreateFromTask(ExploreAppData);
        ExploreAppDataCommand.ThrownExceptions.Subscribe(OnCommmandException);
            
        OpenLogCommand = ReactiveCommand.CreateFromTask(OpenLogAsync);
        OpenLogCommand.ThrownExceptions.Subscribe(OnCommmandException);

        RestartApplicationCommand = ReactiveCommand.CreateFromTask(RestartApplication);
        RestartApplicationCommand.ThrownExceptions.Subscribe(OnCommmandException);

        var canZoomIn = this.WhenAnyValue(x => x.ZoomLevel, (zoomLevel) => zoomLevel < ZoomConstants.MAX_ZOOM_LEVEL);
        ZoomInCommand = ReactiveCommand.Create(() => _zoomService.ApplicationZoomIn(), canZoomIn);
            
        var canZoomOut = this.WhenAnyValue(x => x.ZoomLevel, (zoomLevel) => zoomLevel > ZoomConstants.MIN_ZOOM_LEVEL);
        ZoomOutCommand = ReactiveCommand.Create(() => _zoomService.ApplicationZoomIn(), canZoomOut);

        OpenPrivacyCommand = ReactiveCommand.CreateFromTask(OpenPrivacy);
        OpenTermsOfUseCommand = ReactiveCommand.CreateFromTask(OpenTermsOfUse);
        
        // ZoomLevelValue = $"{_signinManager.ZoomLevel} %";
        // ZoomLevel = _applicationSettingsManager.GetCurrentApplicationSettings().ZoomLevel;

        // SelectedThemeName = _themeManager.SelectedTheme!.Name;

        // this.RaiseAndSetIfChanged(ref _mySelectedTheme, _themeManager.SelectedTheme);

        AvailableThemesNames = new ObservableCollection<string>(_themeService.AvailableThemes
            .Select(t => t.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList());

        // SelectedThemeName = _themeService.SelectedTheme!.Name;
        //
        // IsDarkMode = _themeService.SelectedTheme!.Mode == ThemeModes.Dark;

        this.WhenAnyValue(x => x.SelectedThemeName)
            .Where(x => x != null)
            .Skip(1)
            .Subscribe(_ => UpdateTheme());
        
        this.WhenAnyValue(x => x.IsDarkMode)
            .Skip(1)
            // .Delay(TimeSpan.FromMilliseconds(200))
            // .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateTheme());
        
        this.WhenActivated(disposables =>
        {
            _zoomService.ZoomLevel
                .ToPropertyEx(this, x => x.ZoomLevel)
                .DisposeWith(disposables);
            
            
            _themeService.SelectedTheme
                .Subscribe(t =>
                {
                    SelectedThemeName = t.Name;
                    IsDarkMode = t.IsDarkMode;
                })
                .DisposeWith(disposables);
            
            // _themeService.SelectedTheme
            //     .Select(t => t.Name)
            //     .ToProperty(this, x => x.SelectedThemeName)
            //     .DisposeWith(disposables);
            //
            // _themeService.SelectedTheme
            //     .Select(t => t.IsDarkMode)
            //     .ToPropertyEx(this, x => x.IsDarkMode)
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<GenericEventArgs<int>>(_uxEventsHub, nameof(_uxEventsHub.ZoomLevelChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(args => OnZoomLevelChanged(args.EventArgs.Value))
            //     .DisposeWith(disposables);
        });
    }

    public ReactiveCommand<Unit, Unit> VisitPowSoftwareCommand { get; }
        
    private ReactiveCommand<Unit, Unit> RestartApplicationCommand { get; }
    public ReactiveCommand<Unit, Unit> ExploreAppDataCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLogCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

    public ReactiveCommand<Unit, Unit> OpenPrivacyCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenTermsOfUseCommand { get; }
    
    public extern int ZoomLevel { [ObservableAsProperty] get; }

    [Reactive]
    internal SelectLocaleViewModel Locale { get; set; }
    
    public ObservableCollection<string> AvailableThemesNames { get; set; }
    
    [Reactive]
    public string? SelectedThemeName { get; set; }

    [Reactive]
    public bool IsDarkMode { get; set; }

    private async Task VisitPowSoftware()
    {
        await _webAccessor.OpenByteSyncWebSite();
    }

    private async Task ExploreAppData()
    {
        await _fileSystemAccessor.OpenDirectory(_localApplicationDataManager.ApplicationDataPath);
    }

    private async Task OpenLogAsync()
    {
        var logFilePath = _localApplicationDataManager.DebugLogFilePath;

        if (logFilePath.IsNullOrEmpty(true))
        {
            logFilePath = _localApplicationDataManager.LogFilePath;
        }

        if (logFilePath != null)
        {
            await _fileSystemAccessor.OpenFile(logFilePath);
        }
        else
        {
            _logger.LogError("GeneralSettingsViewModel.OpenLogAsync: Unable to find log file path");
        }
    }
        
    private async Task RestartApplication()
    {
        await _applicationRestarter.RestartAndScheduleShutdown(3);
    }

    // private void ZoomIn()
    // {
    //     _applicationSettingsManager.ApplicationZoomIn();
    // }
    //
    // private void ZoomOut()
    // {
    //     _applicationSettingsManager.ApplicationZoomOut();
    // }

    private async Task OpenPrivacy()
    {
        await _webAccessor.OpenPrivacy();
    }

    private async Task OpenTermsOfUse()
    {
        await _webAccessor.OpenTermsOfUse();
    }

    private void UpdateTheme()
    {
        _themeService.SelectTheme(SelectedThemeName, IsDarkMode);
        
        // var selectedTheme = _themeService.SelectedTheme!;
        //
        // if (!selectedTheme.Name.Equals(SelectedThemeName) || selectedTheme.IsDarkMode != IsDarkMode)
        // {
        //     _themeService.SelectTheme(SelectedThemeName, IsDarkMode);
        // }
        
        
        // if (SelectedTheme != null && !SelectedTheme.Equals(_themeManager.SelectedTheme))
        // {
        //     _themeManager.ChangeTheme(SelectedTheme);
        //
        //     _applicationSettingsManager.SaveApplicationSettings();
        // }
    }

    // private void OnZoomLevelChanged(int obj)
    // {
    //     ZoomLevel = obj;
    //     // ZoomLevelValue = $"{obj} %";
    // }
        
    private void OnCommmandException(Exception exception)
    {
        _logger.LogError(exception, "An error has occured");
    }
   
}