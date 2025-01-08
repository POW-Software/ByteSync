using System.Reflection;
using Autofac;
using ByteSync.Services.Communications;
using ByteSync.Services.Encryptions;
using Module = Autofac.Module;

namespace ByteSync.DependencyInjection.Modules;

public class bak_ServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // // Enregistrement explicite des services
        // builder.RegisterType<WebAccessor>().AsImplementedInterfaces();
        // builder.RegisterType<DataEncrypter>().AsImplementedInterfaces();
        // // Continuez avec tous les services spécifiques

        // Enregistrement par conventions
        var executingAssembly = Assembly.GetExecutingAssembly();

        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Service") && t.Name != "EnvironmentService")
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Manager"))
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Factory"))
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Converter"))
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Mapper"))
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Holder"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Indexer"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Cache"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("EventsHub"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("ApiClient"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Repository") && t.Name != "ApplicationSettingsRepository")
        //     .SingleInstance()
        //     .AsImplementedInterfaces();

        // Ajoutez d'autres conventions si nécessaire
    }
}