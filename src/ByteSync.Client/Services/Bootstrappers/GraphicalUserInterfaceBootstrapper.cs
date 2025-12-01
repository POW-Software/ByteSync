using Autofac;
using Avalonia;
using ByteSync.Business.Misc;
using ByteSync.Business.Navigations;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Bootstrapping;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Announcements;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services;
using ByteSync.Interfaces.Updates;
using Splat.Autofac;

namespace ByteSync.Services.Bootstrappers;

public class GraphicalUserInterfaceBootstrapper : BaseBootstrapper
{
    private readonly ISearchUpdateService _searchUpdateService;
    private readonly ILocalizationService _localizationService;
    private readonly IZoomService _zoomService;
    private readonly IThemeFactory _themeFactory;
    private readonly INavigationService _navigationService;
    private readonly IConnectionService _connectionService;
    private readonly IDeleteUpdateBackupSnippetsService _deleteUpdateBackupSnippetsService;
    private readonly IPushReceiversStarter _pushReceiversStarter;

    public GraphicalUserInterfaceBootstrapper(IEnvironmentService environmentService, ISearchUpdateService searchUpdateService, 
        ILocalApplicationDataManager localApplicationDataManager, ILocalizationService localizationService, 
        IZoomService zoomService, IThemeFactory themeFactory, INavigationService navigationService,
        IConnectionService connectionService, IDeleteUpdateBackupSnippetsService updateBackupSnippetsDeleter,
        IPushReceiversStarter pushReceiversStarter, ILogger<GraphicalUserInterfaceBootstrapper> logger) 
        : base(environmentService, logger, localApplicationDataManager)
    {
        _searchUpdateService = searchUpdateService;
        _localizationService = localizationService;
        _zoomService = zoomService;
        _themeFactory = themeFactory;
        _navigationService = navigationService;
        _connectionService = connectionService;
        _deleteUpdateBackupSnippetsService = updateBackupSnippetsDeleter;
        _pushReceiversStarter = pushReceiversStarter;
    }
    
    public override void Start()
    {
        if (_environmentService.ExecutionMode == ExecutionMode.Debug)
        {
            AttachConsole?.Invoke();
        }
        
        // LocalApplicationDataManager
        // _localApplicationDataManager.Initialize();
        
        // // LoggerService
        // _loggerService.Initialize();

        // LogBootstrap();
        
        // Application Settings
        // _applicationSettingsRepository.Initialize();

        // UpdateService
        _deleteUpdateBackupSnippetsService.DeleteBackupSnippetsAsync();

        // LocalizationService
        _localizationService.Initialize();

        using (var scope = ContainerProvider.Container.BeginLifetimeScope())
        {
            var announcementService = scope.Resolve<IAnnouncementService>();
            _ = announcementService.Start();

            scope.Resolve<IRatingPromptService>();
        }

        
        
        
        
        // if (!operateCommandLine)
        // {
        //     // ThemeService initialization when GUI is on
        //     _themeService.Initialize();
        // }

        _connectionService.StartConnectionAsync();

        try
        {
            _logger.LogInformation("Operating in desktop application mode");

            _navigationService.NavigateTo(NavigationPanel.Home);

            // if (attachConsoleEvenIfGui)
            // {
            //     AttachConsole?.Invoke();
            // }

            _searchUpdateService.SearchNextAvailableVersionsAsync();
            // _connectionManager.Connect();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(_environmentService.Arguments);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _connectionService.StopConnection();
        }
    }

    
    
    public override void AfterFrameworkInitializationCompleted()
    {
        _pushReceiversStarter.Start();
        
        // ZoomService
        _zoomService.Initialize();
        
        // ThemeService
        _themeFactory.BuildThemes();
    }

    private AppBuilder BuildAvaloniaApp()
    {
        var autofacResolver = ContainerProvider.Container.Resolve<AutofacDependencyResolver>();

        // Set a lifetime scope (either the root or any of the child ones) to Autofac resolver
        // This is needed, because the previous steps did not Update the ContainerBuilder since they became immutable in Autofac 5+
        // https://github.com/autofac/Autofac/issues/811
        autofacResolver.SetLifetimeScope(ContainerProvider.Container);

        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
        
#if LIN
        builder = builder.With(new X11PlatformOptions
        {
            // https://github.com/AvaloniaUI/Avalonia/issues/9383
            UseDBusFilePicker = false
        });
#endif
        
        return builder;
    }
}
