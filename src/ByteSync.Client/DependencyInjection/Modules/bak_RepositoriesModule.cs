using System.Reflection;
using Autofac;
using ByteSync.Interfaces;
using ByteSync.Services.Configurations;
using Module = Autofac.Module;

namespace ByteSync.DependencyInjection.Modules;

public class bak_RepositoriesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // builder.RegisterType<ApplicationSettingsRepository>()
        //     .As<IApplicationSettingsRepository>()
        //     .SingleInstance();
        //
        // builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
        //     .Where(t => t.Name.EndsWith("Repository") && t.Name != "ApplicationSettingsRepository")
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // // Ajoutez d'autres repositories si nécessaire
    }
}