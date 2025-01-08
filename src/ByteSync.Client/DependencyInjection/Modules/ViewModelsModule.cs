using System.Reflection;
using Autofac;
using ByteSync.Business.Navigations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.ViewModels;
using ByteSync.ViewModels.Headers;
using ByteSync.ViewModels.Home;
using ByteSync.ViewModels.Lobbies;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Sessions;
using ReactiveUI;
using Module = Autofac.Module;

namespace ByteSync.DependencyInjection.Modules;

public class ViewModelsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
        //     .Where(t => t.Name.EndsWith("ViewModel"))
        //     .AsSelf()
        //     .InstancePerLifetimeScope();

        builder.RegisterType<MainWindowViewModel>()
            .SingleInstance()
            .AsSelf()
            .AsImplementedInterfaces();

        builder.RegisterType<FlyoutContainerViewModel>()
            .SingleInstance()
            .AsSelf()
            .As<IDialogView>()
            .AsImplementedInterfaces();
        
        builder.RegisterType<HeaderViewModel>().SingleInstance().AsSelf();
        
        builder.RegisterType<HomeMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.Home);
        builder.RegisterType<SessionMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.CloudSynchronization);
        builder.RegisterType<SessionMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.LocalSynchronization);
        builder.RegisterType<LobbyMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.ProfileSessionLobby);
        builder.RegisterType<LobbyMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.ProfileSessionDetails);
    }
}