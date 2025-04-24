using Autofac;
using ByteSync.Business.Configurations;
using ByteSync.Common.Controls;
using ByteSync.Common.Interfaces;
using ByteSync.Interfaces.Repositories;
using ByteSync.Repositories;

namespace ByteSync.DependencyInjection.Modules;

public class GenericTypesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(SessionInvalidationCachePolicy<,>))
            .As(typeof(ISessionInvalidationCachePolicy<,>))
            .InstancePerDependency();
        
        builder.RegisterGeneric(typeof(PropertyIndexer<,>))
            .As(typeof(IPropertyIndexer<,>))
            .InstancePerDependency();
        
        builder.RegisterType<ConfigurationReader<ApplicationSettings>>().As<IConfigurationReader<ApplicationSettings>>();
        builder.RegisterType<ConfigurationWriter<ApplicationSettings>>().As<IConfigurationWriter<ApplicationSettings>>();
    }
}