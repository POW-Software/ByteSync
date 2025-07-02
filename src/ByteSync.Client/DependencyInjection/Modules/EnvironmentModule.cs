using System.IO.Abstractions;
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
        var environmentService = new EnvironmentService();
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