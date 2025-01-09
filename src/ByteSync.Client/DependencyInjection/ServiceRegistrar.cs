using System.Net.Http;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using ByteSync.DependencyInjection.Modules;
using ByteSync.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Services;
using ByteSync.Services.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using ReactiveUI;
using Splat.Autofac;

namespace ByteSync.DependencyInjection;

public static class ServiceRegistrar
{
    public static IContainer RegisterComponents()
    {
        var builder = new ContainerBuilder();
        
        var serviceCollection = new ServiceCollection();
        builder.RegisterModule(new ServiceCollectionModule(serviceCollection));
        
        builder.RegisterModule<ConfigurationModule>();
        builder.RegisterModule<EnvironmentModule>();
        builder.RegisterModule<AutoScanningModule>();
        builder.RegisterModule<GenericTypesModule>();
        builder.RegisterModule<KeyedTypesModule>();
        builder.RegisterModule<SingletonsModule>();
        builder.RegisterModule<ViewModelsModule>();

        var autofacResolver = builder.UseAutofacDependencyResolver();
        builder.RegisterInstance(autofacResolver);
        
        autofacResolver.InitializeReactiveUI();

        var container = builder.Build();
        ContainerProvider.Container = container;

        FeedClientId(container);

        return container;
    }

    private static void FeedClientId(IContainer container)
    {
        using var scope = container.BeginLifetimeScope();
        
        var applicationSettingsRepository = scope.Resolve<IApplicationSettingsRepository>();
        var environmentService = scope.Resolve<IEnvironmentService>();
        
        var applicationSettings = applicationSettingsRepository.GetCurrentApplicationSettings();
        environmentService.SetClientId(applicationSettings.ClientId);
            
        // logger.LogInformation("Test de log après la construction du conteneur.");


        // var applicationSettingsRepository = new ApplicationSettingsRepository(
        //     localApplicationDataManager, configurationReader, configurationWriter);
        // var applicationSettings = applicationSettingsRepository.GetCurrentApplicationSettings();
        // environmentService.SetClientId(applicationSettings.ClientId);
    }
}