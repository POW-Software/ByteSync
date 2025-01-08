using Autofac;
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
    }
}