using Autofac;
using Module = Autofac.Module;

namespace ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;

public class ServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(GlobalTestSetup.ByteSyncServerCommonAssembly)
            .Where(t => t.Name.EndsWith("Service"))
            .AsImplementedInterfaces()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}