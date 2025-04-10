using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Repositories;

public class CloudSessionsRepository : BaseRepository<CloudSessionData>, ICloudSessionsRepository
{
    private readonly IRedisInfrastructureService _redisInfrastructureService;

    public CloudSessionsRepository(IRedisInfrastructureService redisInfrastructureService,
        ICacheRepository<CloudSessionData> cacheRepository) : base(redisInfrastructureService, cacheRepository)
    {
        _redisInfrastructureService = redisInfrastructureService;
    }
    
    private CacheKey ComputeSessionCacheKey(CloudSessionData cloudSessionData)
    {
        return _redisInfrastructureService.ComputeCacheKey(EntityType, cloudSessionData.SessionId);
    }
    
    public override EntityType EntityType => EntityType.Session;

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
            await using var redisLock = await _redisInfrastructureService.AcquireLockAsync(cacheKey);
            
            string? serializedElement = await _redisInfrastructureService.GetDatabase().StringGetAsync(cacheKey.Value);
            if (serializedElement == null || serializedElement.IsEmpty())
            {
                await Save(cacheKey, cloudSessionData, transaction);
                isNewSessionOk = true;
            }
        }

        return cloudSessionData;
    }
}