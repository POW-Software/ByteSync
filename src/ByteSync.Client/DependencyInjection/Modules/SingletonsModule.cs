using Autofac;
using ByteSync.Interfaces.Controls.Bootstrapping;
using ByteSync.Services.Bootstrappers;
using ByteSync.Services.Communications;
using ByteSync.Services.Communications.SignalR;
using ByteSync.Services.Navigations;

namespace ByteSync.DependencyInjection.Modules;

public class SingletonsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CloudProxy>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<NavigationService>().SingleInstance().AsImplementedInterfaces();
        
        builder.RegisterType<HubPushHandler2>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<PublicKeysTruster>().SingleInstance().AsImplementedInterfaces();
        
        builder.RegisterType<PushReceiversStarter>().SingleInstance().AsImplementedInterfaces();
    }
}