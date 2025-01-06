using ByteSync.Business.Arguments;
using ByteSync.Business.Communications;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.ViewModels.TrustedNetworks;
using Serilog;
using Serilog.Events;
using Avalonia;
using ByteSync.Business.Configurations;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Controls.Serilog;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;

namespace ByteSync.Services.Applications;

public class SerilogConfigurationFactory
{
    private readonly ILocalApplicationDataManager _localApplicationDataManager;
    private readonly IEnvironmentService _environmentService;



    public SerilogConfigurationFactory(ILocalApplicationDataManager localApplicationDataManager, IEnvironmentService environmentService)
    {
        _localApplicationDataManager = localApplicationDataManager;
        _environmentService = environmentService;
    }
    

    
    public LoggerConfiguration BuildLoggerConfiguration()
    {
        LogEventLevel logEventLevel = LogEventLevel.Information;
        if (_environmentService.Arguments.Contains(RegularArguments.LOG_DEBUG))
        {
            logEventLevel = LogEventLevel.Debug;
        }

        var loggerConfiguration = new LoggerConfiguration()
    #if DEBUG
            .MinimumLevel.Debug()
    #else
            .MinimumLevel.Information()
    #endif
            .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
            .MinimumLevel.Override("Splat", LogEventLevel.Warning)
            .MinimumLevel.Override("ReactiveUI", LogEventLevel.Warning)
            .MinimumLevel.Override("Avalonia", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With<ExceptionEnricher>()
            .WriteTo.Async(a => a.File(new ConditionalFormatter(),
                IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, LocalApplicationDataConstants.LOGS_DIRECTORY, 
                    "ByteSync_.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: logEventLevel));                  

        if (logEventLevel == LogEventLevel.Debug)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Debug();
        }

        loggerConfiguration = loggerConfiguration
            .Destructure.ByTransforming<TrustedPublicKey>(x => new
                { MachineIdentifier = x.ClientId, x.PublicKeyHash, x.ValidationDate });
        loggerConfiguration = loggerConfiguration
            .Destructure.ByTransforming<PublicKeyCheckData>(x => new
            {
                SenderMachineName = x.IssuerPublicKeyInfo.ClientId,
                SenderPublicKeyHash = x.IssuerPublicKeyHash
            });
        loggerConfiguration = loggerConfiguration
            .Destructure.ByTransforming<PublicKeyInfo>(x => new
            {
                ClientId = x.ClientId,
                PublicKeyHash = new PublicKeyFormatter().Format(x.PublicKey)
            });

        bool writeToConsole = false;
    #if DEBUG
        writeToConsole = true;
    #endif
        
        // Mode Console / Command Line si Application.Current == null
        if (Application.Current == null)
        {
            writeToConsole = true;
        }

        if (writeToConsole)
        {
        #if DEBUG
            loggerConfiguration = loggerConfiguration.WriteTo.Async(a => a.Console());
        #else
            loggerConfiguration = loggerConfiguration.WriteTo.Async(a => a.Console(theme:Serilog.Sinks.SystemConsole.Themes.ConsoleTheme.None));
        #endif
        }
    #if DEBUG
        loggerConfiguration = loggerConfiguration
            .WriteTo.Async(a => a.File(new ConditionalFormatter(),
                IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, LocalApplicationDataConstants.LOGS_DIRECTORY, 
                    "ByteSync_debug_.log"),
                rollingInterval: RollingInterval.Day));
    #endif

        return loggerConfiguration;
    }
    
    // public void Initialize()
    // {
    //     // https://blog.datalust.co/serilog-tutorial/
    //     // https://stackoverflow.com/questions/53515182/lower-log-level-for-quartz
    //
    //     
    //
    //     //Log.Logger = loggerConfiguration.CreateLogger();
    //
    //     Log.Information($"*************************************************");
    //     Log.Information($"***     ByteSync - Application Startup       ****");
    //     Log.Information($"*************************************************");
    //     Log.Information($"Version: {_environmentService.CurrentVersion}");
    //     Log.Information($"MachineName: {_environmentService.MachineName}");
    //     Log.Information(
    //         $"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}{(Environment.Is64BitOperatingSystem ? " (64 bits)" : "")}");
    //     Log.Information($"AssemblyFullName: {_environmentService.AssemblyFullName}");
    //     Log.Information($"DeploymentMode: {(_environmentService.IsPortableApplication ? "Portable" : "Installed")}");
    //     Log.Information($"*************************************************");
    //
    // #if DEBUG
    //     Log.Information(" | Running in DEBUG Mode |");
    //     Log.Information($"*************************************************");
    // #endif
    //
    //     Log.Information("Command Line Arguments:");
    //     var commandLineArgs = Environment.GetCommandLineArgs();
    //     for (int i = 0; i < commandLineArgs.Length; i++)
    //     {
    //         Log.Information(" - Argument {i}: {arg}", i + 1, commandLineArgs[i]);
    //     }
    //
    //     Log.Information($"*************************************************");
    //
    //     if (Environment.GetCommandLineArgs().Contains(RegularArguments.LOG_DEBUG))
    //     {
    //         Log.Information("LoggingLevel: Debug ({Arg})", RegularArguments.LOG_DEBUG);
    //         Log.Information($"*************************************************");
    //     }
    //
    //     Log.Information("ApplicationDataPath: '{applicationDataPath}'", _localApplicationDataManager.ApplicationDataPath);
    //
    //     LogSpecialFolders();
    // }
    //
    // private void LogSpecialFolders()
    // {
    //     // 23/03/2022 Journalisation DEBUG pour obtenir les chemins sur macOS et Linux, à retirer ultérieurement
    //     string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
    //     string commonProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
    //     string commonProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
    //     string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    //     string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    //     string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    //     string commonApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    //     Log.Debug("programs: '{programs}'", programs);
    //     Log.Debug("cprogramFiles: '{cprogramFiles}'", commonProgramFiles);
    //     Log.Debug("cprogramFilesX86: '{cprogramFilesX86}'", commonProgramFilesX86);
    //     Log.Debug("programFiles: '{programFiles}'", programFiles);
    //     Log.Debug("programFilesX86: '{programFilesX86}'", programFilesX86);
    //     Log.Debug("globalApplicationDataPath: '{globalApplicationDataPath}'", applicationData);
    //     Log.Debug("commonApplicationDataPath: '{commonApplicationDataPath}'", commonApplicationData);
    // }
}