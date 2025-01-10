using Autofac;
using ByteSync.Common.Controls;
using ByteSync.Common.Interfaces;
using Prism.Events;

namespace ByteSync.DependencyInjection.Modules;

public class ExternalAssembliesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<EventAggregator>().As<IEventAggregator>();
        
        builder.RegisterType<FileSystemAccessor>().As<IFileSystemAccessor>();
    }
}