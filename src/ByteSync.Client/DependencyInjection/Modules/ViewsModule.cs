using Autofac;
using ByteSync.Interfaces;
using ByteSync.Views;
using Module = Autofac.Module;

namespace ByteSync.DependencyInjection.Modules;

public class ViewsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MainWindow>()
            .SingleInstance()
            .AsSelf()
            .As<IFileDialogService>()
            .AsImplementedInterfaces();
    }
}