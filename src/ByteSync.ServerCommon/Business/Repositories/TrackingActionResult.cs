using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Business.Repositories;

public class TrackingActionResult
{
    public TrackingActionResult(bool isSuccess, List<TrackingActionEntity> trackingActionEntities, SynchronizationEntity synchronization)
    {
        IsSuccess = isSuccess;
        TrackingActionEntities = trackingActionEntities;
        SynchronizationEntity = synchronization;
    }

    public bool IsSuccess { get; }
    public List<TrackingActionEntity> TrackingActionEntities { get; }
    public SynchronizationEntity SynchronizationEntity { get; }
}