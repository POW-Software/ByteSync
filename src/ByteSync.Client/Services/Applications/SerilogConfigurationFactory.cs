using Avalonia;
using ByteSync.Business.Arguments;
using ByteSync.Business.Communications;
using ByteSync.Business.Configurations;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Controls.Serilog;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.ViewModels.TrustedNetworks;
using Serilog;
using Serilog.Events;

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
        var logEventLevel = LogEventLevel.Information;
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
            .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
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

        var writeToConsole = false;
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
            loggerConfiguration =
 loggerConfiguration.WriteTo.Async(a => a.Console(theme:Serilog.Sinks.SystemConsole.Themes.ConsoleTheme.None));
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
}