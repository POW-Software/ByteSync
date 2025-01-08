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
            .WithParameter("environmentService", new EnvironmentService());

        var localApplicationDataManager = new LocalApplicationDataManager(environmentService);
        builder.RegisterInstance(localApplicationDataManager).As<ILocalApplicationDataManager>().SingleInstance();

        var loggerService = new SerilogConfigurationFactory(localApplicationDataManager, environmentService);
        var loggerConfiguration = loggerService.BuildLoggerConfiguration();
        builder.RegisterSerilog(loggerConfiguration);
        
        builder.RegisterType<FileSystemAccessor>().AsImplementedInterfaces();


        var configurationReader = new ConfigurationReader<ApplicationSettings>(fileSystem);
        builder.RegisterInstance<IConfigurationReader<ApplicationSettings>>(configurationReader);

        var configurationWriter = new ConfigurationWriter<ApplicationSettings>(fileSystem);
        builder.RegisterInstance<IConfigurationWriter<ApplicationSettings>>(configurationWriter);

        var applicationSettingsRepository = new ApplicationSettingsRepository(
            localApplicationDataManager, configurationReader, configurationWriter);
        var applicationSettings = applicationSettingsRepository.GetCurrentApplicationSettings();
        environmentService.SetClientId(applicationSettings.ClientId);

        builder.RegisterInstance(applicationSettingsRepository).As<IApplicationSettingsRepository>();
        
        builder.RegisterType<GraphicalUserInterfaceBootstrapper>().Keyed<IBootstrapper>(OperationMode.GraphicalUserInterface);
        builder.RegisterType<CommandLineBootstrapper>().Keyed<IBootstrapper>(OperationMode.CommandLine);
        
        builder.RegisterType<EventAggregator>().As<IEventAggregator>();
    }
}