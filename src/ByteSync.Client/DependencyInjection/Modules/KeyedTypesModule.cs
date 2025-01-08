using Autofac;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Services.Communications.Transfers.AfterTransfers;
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
    }
}