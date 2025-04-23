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
            .As(typeof(ISessionInvalidationSourceCachePolicy<,>))
            .InstancePerDependency();
        
        builder.RegisterGeneric(typeof(IndexedCache<,>))
            .As(typeof(IIndexedCache<,>))
            .InstancePerDependency();
        
        builder.RegisterType<ConfigurationReader<ApplicationSettings>>().As<IConfigurationReader<ApplicationSettings>>();
        builder.RegisterType<ConfigurationWriter<ApplicationSettings>>().As<IConfigurationWriter<ApplicationSettings>>();
    }
}