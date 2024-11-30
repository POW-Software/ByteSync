using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Mappers;

public interface ISynchronizationMapper
{
    Synchronization MapToSynchronization(SynchronizationEntity synchronizationEntity);
}