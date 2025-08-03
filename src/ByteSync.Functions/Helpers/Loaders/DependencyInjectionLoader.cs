using System.Reflection;
using Autofac;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using Microsoft.Azure.SignalR.Management;

namespace ByteSync.Functions.Helpers.Loaders;

public static class DependencyInjectionLoader
{
    public static void LoadDependencyInjection(this ContainerBuilder builder)
    {
        RegisterCurrentAssembly(builder);

        RegisterServerCommonAssembly(builder);
    }

    private static void RegisterServerCommonAssembly(ContainerBuilder builder)
    {
        var executingAssembly = typeof(IClientsRepository).Assembly;
    
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Repository"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
        
        var genericRepositoryTypes = executingAssembly.GetTypes()
            .Where(t => t.Name.Contains("Repository`") && t.IsGenericTypeDefinition && !t.IsInterface);
            
        foreach (var genericType in genericRepositoryTypes)
        {
            builder.RegisterGeneric(genericType)
                .AsImplementedInterfaces()
                .AsSelf()
                .InstancePerLifetimeScope();
        }
    
        builder.Register(c =>
        {
            var factory = c.Resolve<IHttpClientFactory>();
            return factory.CreateClient();
        }).As<HttpClient>().InstancePerLifetimeScope();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Service"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
    
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Factory"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
    
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Loader"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
    
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Mapper"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
    
        builder.Register(c => {
            var factory = c.Resolve<IHubContextFactory>();
            return factory.CreateHubContext();
        }).As<ServiceHubContext<IHubByteSyncPush>>().SingleInstance().AsImplementedInterfaces();
    }

    private static void RegisterCurrentAssembly(ContainerBuilder builder)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();

        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Service"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
    }
}