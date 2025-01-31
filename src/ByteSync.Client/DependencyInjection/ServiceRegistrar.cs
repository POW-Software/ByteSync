using Autofac;
using ByteSync.Business.Arguments;
using ByteSync.Common.Interfaces;
using ByteSync.DependencyInjection.Modules;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Bootstrapping;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Services;
using Microsoft.Extensions.DependencyInjection;
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
        
        builder.RegisterModule<AutoDetectionModule>();
        builder.RegisterModule<ConfigurationModule>();
        builder.RegisterModule<EnvironmentModule>();
        builder.RegisterModule<GenericTypesModule>();
        builder.RegisterModule<KeyedTypesModule>();
        builder.RegisterModule<SingletonsModule>();
        builder.RegisterModule<ViewModelsModule>();
        builder.RegisterModule<ViewsModule>();
        builder.RegisterModule<ExternalAssembliesModule>();

        var autofacResolver = builder.UseAutofacDependencyResolver();
        builder.RegisterInstance(autofacResolver);
        
        autofacResolver.InitializeReactiveUI();

        var container = builder.Build();
        ContainerProvider.Container = container;

        using (var scope = container.BeginLifetimeScope())
        {
            var environmentService = scope.Resolve<IEnvironmentService>();
            if (environmentService.Arguments.Contains(RegularArguments.VERSION))
            {
                return container;
            }
        }

        LogBootstrapHeader(container);
        
        FeedClientId(container);

        LogBootstrap(container);

        return container;
    }

    private static void FeedClientId(IContainer container)
    {
        using var scope = container.BeginLifetimeScope();
        
        var applicationSettingsRepository = scope.Resolve<IApplicationSettingsRepository>();
        var environmentService = scope.Resolve<IEnvironmentService>();
        
        var applicationSettings = applicationSettingsRepository.GetCurrentApplicationSettings();
        environmentService.SetClientId(applicationSettings.ClientId);
    }
    
    private static void LogBootstrapHeader(IContainer container)
    {
        using var scope = container.BeginLifetimeScope();
        
        var logger = scope.Resolve<IBootstrapLogger>();
        logger.LogBootstrapHeader();
    }
    
    private static void LogBootstrap(IContainer container)
    {
        using var scope = container.BeginLifetimeScope();
        
        var logger = scope.Resolve<IBootstrapLogger>();
        logger.LogBootstrapContent();
    }
}