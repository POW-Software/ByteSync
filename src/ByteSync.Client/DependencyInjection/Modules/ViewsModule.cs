using Autofac;
using Avalonia.ReactiveUI;
using ByteSync.Interfaces;
using ByteSync.ViewModels.Home;
using ByteSync.ViewModels.Lobbies;
using ByteSync.ViewModels.Sessions;
using ByteSync.ViewModels.Announcements;
using ByteSync.ViewModels.Ratings;
using ByteSync.Views;
using ByteSync.Views.Home;
using ByteSync.Views.Announcements;
using ByteSync.Views.Lobbies;
using ByteSync.Views.Sessions;
using ByteSync.Views.Ratings;
using ReactiveUI;
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
        
        builder.RegisterType<HomeMainView>().As<IViewFor<HomeMainViewModel>>();
        builder.RegisterType<SessionMainView>().As<IViewFor<SessionMainViewModel>>();
        builder.RegisterType<LobbyMainView>().As<IViewFor<LobbyMainViewModel>>();
        builder.RegisterType<AnnouncementView>().As<IViewFor<AnnouncementViewModel>>();
        builder.RegisterType<RatingPromptView>().As<IViewFor<RatingPromptViewModel>>();
        
        builder.RegisterInstance(new AvaloniaActivationForViewFetcher())
            .As<IActivationForViewFetcher>()
            .SingleInstance();

        builder.RegisterInstance(new AutoDataTemplateBindingHook())
            .As<IPropertyBindingHook>()
            .SingleInstance();
    }
}
