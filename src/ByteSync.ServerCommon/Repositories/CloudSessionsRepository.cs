using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Repositories;

public class CloudSessionsRepository : BaseRepository<CloudSessionData>, ICloudSessionsRepository
{
    public CloudSessionsRepository(ICacheService cacheService) : base(cacheService)
    {

    }
    
    private string ComputeSessionCacheKey(CloudSessionData cloudSessionData)
    {
        return ComputeCacheKey("Session", cloudSessionData.SessionId);
    }
    
    public override string ElementName => "Session";

    public Task<SessionMemberData?> GetSessionMember(string sessionId, Client client)
    {
        return GetSessionMember(sessionId, client.ClientInstanceId);
    }

    public async Task<SessionMemberData?> GetSessionMember(string sessionId, string clientInstanceId)
    {
        var cloudSessionData = await Get(sessionId);

        return cloudSessionData?.SessionMembers.SingleOrDefault(sm => sm.ClientInstanceId == clientInstanceId);
    }

    public async Task<SessionMemberData?> GetSessionPreMember(string sessionId, string clientInstanceId)
    {
        var cloudSessionData = await Get(sessionId);

        return cloudSessionData?.PreSessionMembers.SingleOrDefault(sm => sm.ClientInstanceId == clientInstanceId);
    }

    public async Task<CloudSessionData> AddCloudSession(CloudSessionData cloudSessionData, Func<string> generateSessionIdHandler, ITransaction transaction)
    {
        bool isNewSessionOk = false;

        while (!isNewSessionOk) 
        {
            cloudSessionData.SessionId = generateSessionIdHandler.Invoke();
        
            var cacheKey = ComputeSessionCacheKey(cloudSessionData);
            await using var redisLock = await _cacheService.AcquireLockAsync(cacheKey);
            
            string? serializedElement = await transaction.StringGetAsync(cacheKey);
            if (serializedElement == null || serializedElement.IsEmpty())
            {
                await SetElement(cacheKey, cloudSessionData, transaction);
                isNewSessionOk = true;
            }
        }

        return cloudSessionData;
    }
}