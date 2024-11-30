using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;

namespace ByteSync.Services.Bootstrappers;

public class CommandLineBootstrapper : BaseBootstrapper
{
    public CommandLineBootstrapper(IEnvironmentService environmentService, 
        ILocalApplicationDataManager localApplicationDataManager, ILogger<CommandLineBootstrapper> logger) 
        : base(environmentService, logger, localApplicationDataManager)
    {

    }
    
    public override void Start()
    {
        throw new System.NotImplementedException();
        
        /*
        
        bool operateCommandLine = _environmentService.OperateCommandLine;

        // AttachConsole 
        if ((_environmentService.OperateCommandLine && isWindows)
            || !_environmentService.OperateCommandLine && consoleEvenIfGui)
        {
            AttachConsole?.Invoke();
        }
        
        // LocalApplicationDataManager
        _localApplicationDataManager.Initialize();
        
        // LoggerService
        _loggerService.Initialize();
        
        // Application Settings
        _applicationSettingsRepository.Initialize();

        // UpdateService
        _updateService.DeleteBackupSnippetsAsync();

        // LocalizationService
        _localizationService.Initialize();

        
        
        
        
        // if (!operateCommandLine)
        // {
        //     // ThemeService initialization when GUI is on
        //     _themeService.Initialize();
        // }

        _ = _connectionService.StartConnectionAsync();

        try
        {
            // UpdateService: search next versions
            // _updateService.SearchNextAvailableVersionsAsync().GetAwaiter().GetResult();
            // _connectionManager.Connect().GetAwaiter().GetResult();
            // .ContinueWith(_ => _connectionManager.Connect()).GetAwaiter().GetResult();

            if (operateCommandLine)
            {
                _updateService.SearchNextAvailableVersionsAsync().GetAwaiter().GetResult();
                // _connectionManager.Connect().GetAwaiter().GetResult();

                // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                // {
                //     // Pour Windows, il faut <OutputType>WinExe</OutputType> dans ByteSync.csproj
                //     // Or avec ça, Windows ne peut pas écrire dans la console
                //     // Si on veut écrire dans la console, il faut donc l'attacher
                //     
                //     // Si on passe en <OutputType>Exe</OutputType>, on peut écrire dans la console,
                //     // mais une fenêtre de console s'affiche systématiquement au lancement de l'application dans Windows
                //     
                //     // Définition #if WIN : https://stackoverflow.com/questions/30153797/c-sharp-preprocessor-differentiate-between-operating-systems
                //
                //     AttachConsole?.Invoke();
                // }



                // var appBuilder = BuildAvaloniaApp();
                // appBuilder.SetupWithoutStarting();


                Log.Information("Operating in command line mode");

                // todo : passer cela en méthode dans BootStrapper d'après initlialisation de la journalisation
                // todo : y passer aussi la recherche de mise à jour
                // IUpdateManager updateManager = Locator.Current.GetService<IUpdateManager>()!;
                // _updateManager.DeleteBackupSnippetsAsync();

                // var uiHelper = Locator.Current.GetService<IUIHelper>()!;
                // uiHelper.IsCommandLineMode = true;

                // CommandLineModeHandler commandLineModeHandler = new CommandLineModeHandler();
                var task = Task.Run(() => _commandLineModeHandler.Operate());
                task.Wait();

                Log.CloseAndFlush();
            }
            else
            {


                Log.Information("Operating in desktop application mode");

                _navigationService.NavigateTo(NavigationPanel.Home);

                // if (attachConsoleEvenIfGui)
                // {
                //     AttachConsole?.Invoke();
                // }

                _updateService.SearchNextAvailableVersionsAsync();
                // _connectionManager.Connect();

                BuildAvaloniaApp().StartWithClassicDesktopLifetime(programArgs);
            }
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
        */
    }
    
    public override void AfterFrameworkInitializationCompleted()
    {
        /*
        // ZoomService
        _zoomService.Initialize();
        
        // ThemeService
        _themeService.Initialize();
        */
    }
}