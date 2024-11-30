using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Mappers;

namespace ByteSync.ServerCommon.Mappers;

public class TrackingActionMapper : ITrackingActionMapper
{
    public TrackingActionSummary MapToTrackingActionSummary(TrackingActionEntity trackingActionEntity)
    {
        return new TrackingActionSummary
        {
            ActionsGroupId = trackingActionEntity.ActionsGroupId,
            IsSuccess = trackingActionEntity.IsSuccess,
            IsError = trackingActionEntity.IsError
        };
    }
}