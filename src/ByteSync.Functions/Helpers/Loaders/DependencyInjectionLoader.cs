using System.Reflection;
using Autofac;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Hubs;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Storage;
using Microsoft.Azure.SignalR.Management;

namespace ByteSync.Functions.Helpers.Loaders;

public static class DependencyInjectionLoader
{
    public static void LoadDependencyInjection(this ContainerBuilder builder)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        
            builder.RegisterAssemblyTypes(executingAssembly)
                .Where(t => t.Name.EndsWith("Service"))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();

            // ByteSync.ServerCommon
            executingAssembly = typeof(IClientsRepository).Assembly;
        
            builder.RegisterAssemblyTypes(executingAssembly)
                .Where(t => t.Name.EndsWith("Repository"))
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();
        
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
        
            builder.RegisterType<ClientsGroupsInvoker>()
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();
            
            builder.RegisterType<ClientsGroupsManager>()
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();
        
            builder.RegisterType<BlobContainerProvider>()
                .InstancePerLifetimeScope()
                .AsImplementedInterfaces();
        
            builder.Register(c => {
                var factory = c.Resolve<IHubContextFactory>();
                return factory.CreateHubContext();
            }).As<ServiceHubContext<IHubByteSyncPush>>().SingleInstance().AsImplementedInterfaces();
    }
}