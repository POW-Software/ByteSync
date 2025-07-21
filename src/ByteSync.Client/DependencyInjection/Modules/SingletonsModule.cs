using System.IO.Abstractions;
using Autofac;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Services.Automating;
using ByteSync.Services.Bootstrappers;
using ByteSync.Services.Communications;
using ByteSync.Services.Communications.SignalR;
using ByteSync.Services.Communications.Transfers;
using ByteSync.Services.Inventories;
using ByteSync.Services.Navigations;
using ByteSync.Services.Sessions;
using ByteSync.Services.Sessions.Connecting;
using ByteSync.Services.Synchronizations;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.DependencyInjection.Modules;

public class SingletonsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<FileSystem>().SingleInstance().As<IFileSystem>();
        
        builder.RegisterType<CloudProxy>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<NavigationService>().SingleInstance().AsImplementedInterfaces();
        
        builder.RegisterType<HubPushHandler2>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<PublicKeysTruster>().SingleInstance().AsImplementedInterfaces();
        
        builder.RegisterType<PushReceiversStarter>().SingleInstance().AsImplementedInterfaces();
        
        builder.RegisterType<DataInventoryStarter>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<CloudSessionConnectionService>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<FileDownloaderCache>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<DownloadTargetBuilder>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<CommandLineModeHandler>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationStarter>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationActionServerInformer>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<DownloadManager>().SingleInstance().AsImplementedInterfaces();
    }
}