using System.IO.Abstractions;
using Autofac;
using ByteSync.Business.Configurations;
using ByteSync.Business.Misc;
using ByteSync.Common.Controls;
using ByteSync.Common.Interfaces;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Bootstrapping;
using ByteSync.Services.Applications;
using ByteSync.Services.Bootstrappers;
using ByteSync.Services.Configurations;
using Prism.Events;
using Serilog.Extensions.Autofac.DependencyInjection;

namespace ByteSync.DependencyInjection.Modules;

public class EnvironmentModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var fileSystem = new FileSystem();
        builder.RegisterInstance<IFileSystem>(fileSystem).SingleInstance();
        
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
        
        builder.RegisterType<GraphicalUserInterfaceBootstrapper>().Keyed<IBootstrapper>(OperationMode.GraphicalUserInterface);
        builder.RegisterType<CommandLineBootstrapper>().Keyed<IBootstrapper>(OperationMode.CommandLine);
        
        builder.RegisterType<EventAggregator>().As<IEventAggregator>();
    }
}