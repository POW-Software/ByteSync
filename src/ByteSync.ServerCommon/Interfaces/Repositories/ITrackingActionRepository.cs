using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface ITrackingActionRepository : IRepository<TrackingActionEntity>
{
    Task<TrackingActionEntity> GetOrBuild(string sessionId, string key);
    
    Task<TrackingActionResult> AddOrUpdate(string sessionId, List<string> actionsGroupIds,
        Func<TrackingActionEntity, SynchronizationEntity, bool> updateHandler);
}