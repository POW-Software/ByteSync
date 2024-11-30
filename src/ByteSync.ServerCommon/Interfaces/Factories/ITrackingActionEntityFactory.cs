using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Factories;

public interface ITrackingActionEntityFactory
{
    Task<TrackingActionEntity> Create(string sessionId, string actionsGroupId);
}