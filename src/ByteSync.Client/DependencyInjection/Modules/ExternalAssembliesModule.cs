using Autofac;
using ByteSync.Common.Controls;
using ByteSync.Common.Interfaces;

namespace ByteSync.DependencyInjection.Modules;

public class ExternalAssembliesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        
        builder.RegisterType<FileSystemAccessor>().As<IFileSystemAccessor>();
    }
}