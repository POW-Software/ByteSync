using Autofac;
using Module = Autofac.Module;

namespace ByteSync.Functions.IntegrationTests.Helpers.Autofac;

public class FactoriesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(GlobalTestSetup.ByteSyncServerCommonAssembly)
            .Where(t => t.Name.EndsWith("Factory"))
            .AsImplementedInterfaces()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}