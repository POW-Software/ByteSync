using System.Reflection;
using Autofac;
using ByteSync.Interfaces.Communications;
using Module = Autofac.Module;

namespace ByteSync.DependencyInjection.Modules;

public class AutoScanningModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.IsClass
                        && !t.IsAbstract 
                        && t.GetInterfaces().Any()
                        && t.Namespace != null 
                        && t.Namespace.StartsWith("ByteSync.Services"))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Service") && t.Name != "EnvironmentService")
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Manager"))
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Factory"))
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Converter"))
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Mapper"))
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Holder"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Indexer"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Cache"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("EventsHub"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("ApiClient"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Repository"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AssignableTo<IPushReceiver>()
            .As<IPushReceiver>()
            .SingleInstance();
        
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Proxy"));
    }
}