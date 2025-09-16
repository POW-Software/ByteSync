using Autofac;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Services.Applications;
using ByteSync.Services.Configurations;
using Serilog.Extensions.Autofac.DependencyInjection;

namespace ByteSync.DependencyInjection.Modules;

public class EnvironmentModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register PFN parser and environment service
        var msixPfnParser = new MsixPfnParser();
        builder.RegisterInstance(msixPfnParser).As<IMsixPfnParser>();
        
        var environmentService = new EnvironmentService(msixPfnParser);
        builder.RegisterInstance(environmentService).As<IEnvironmentService>();
        
        builder.RegisterType<LocalApplicationDataManager>()
            .As<ILocalApplicationDataManager>()
            .SingleInstance()
            .WithParameter("environmentService", environmentService);
        
        var localApplicationDataManager = new LocalApplicationDataManager(environmentService);
        builder.RegisterInstance(localApplicationDataManager).As<ILocalApplicationDataManager>().SingleInstance();
        
        var loggerService = new SerilogConfigurationFactory(localApplicationDataManager, environmentService);
        var loggerConfiguration = loggerService.BuildLoggerConfiguration();
        builder.RegisterSerilog(loggerConfiguration);
    }
}