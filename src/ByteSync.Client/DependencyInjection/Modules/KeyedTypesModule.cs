using Autofac;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Bootstrapping;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Services.Bootstrappers;
using ByteSync.Services.Communications.Transfers.AfterTransfers;
using ByteSync.Services.Communications.Transfers.Strategies;
using ByteSync.Services.TimeTracking;

namespace ByteSync.DependencyInjection.Modules;

public class KeyedTypesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<InventoryDataTrackingStrategy>()
            .Keyed<IDataTrackingStrategy>(TimeTrackingComputerType.Inventory);
        builder.RegisterType<SynchronizationDataTrackingStrategy>()
            .Keyed<IDataTrackingStrategy>(TimeTrackingComputerType.Synchronization);
        
        builder.RegisterType<AfterTransferSynchronizationSharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.DeltaSynchronization);
        builder.RegisterType<AfterTransferSynchronizationSharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.FullSynchronization);
        builder.RegisterType<AfterTransferInventorySharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.BaseInventory);
        builder.RegisterType<AfterTransferInventorySharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.FullInventory);
        
        builder.RegisterType<GraphicalUserInterfaceBootstrapper>().Keyed<IBootstrapper>(OperationMode.GraphicalUserInterface);
        builder.RegisterType<CommandLineBootstrapper>().Keyed<IBootstrapper>(OperationMode.CommandLine);
        
        builder.RegisterType<BlobStorageDownloadStrategy>().Keyed<IDownloadStrategy>(StorageProvider.AzureBlobStorage);
        builder.RegisterType<CloudFlareDownloadStrategy>().Keyed<IDownloadStrategy>(StorageProvider.CloudflareR2);

        builder.RegisterType<BlobStorageUploadStrategy>().Keyed<IUploadStrategy>(StorageProvider.AzureBlobStorage);
        builder.RegisterType<CloudFlareUploadStrategy>().Keyed<IUploadStrategy>(StorageProvider.CloudflareR2);
    }
}