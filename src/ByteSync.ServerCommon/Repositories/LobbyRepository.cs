using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Repositories;

public class LobbyRepository : BaseRepository<Lobby>, ILobbyRepository
{
    private readonly IRedisInfrastructureService _redisInfrastructureService;

    public LobbyRepository(IRedisInfrastructureService redisInfrastructureService,
        ICacheRepository<Lobby> cacheRepository) : base(redisInfrastructureService, cacheRepository)
    {
        _redisInfrastructureService = redisInfrastructureService;
    }
    
    public override EntityType EntityType { get; } = EntityType.Lobby;

    public async Task<UpdateEntityResult<Lobby>> QuitLobby(string lobbyId, string clientInstanceId, ITransaction transaction)
    {
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType, lobbyId);
        await using var redisLock = await _redisInfrastructureService.AcquireLockAsync(cacheKey);

        var lobby = await Get(cacheKey);

        bool updateLobby = false;
        bool deleteLobby = false;
        if (lobby != null)
        {
            var lobbyMemberCell = lobby.LobbyMemberCells.SingleOrDefault(lmc => lmc.LobbyMember?.ClientInstanceId == clientInstanceId);

            if (lobbyMemberCell != null)
            {
                lobbyMemberCell.LobbyMember = null;
                updateLobby = true;

                if (lobby.ConnectedLobbyMembers.Count == 0)
                {
                    deleteLobby = true;
                }
            }
        }

        if (deleteLobby)
        {
            await transaction.KeyDeleteAsync(cacheKey.Value);

            return new UpdateEntityResult<Lobby>(lobby, UpdateEntityStatus.Deleted);
        }
        else if (updateLobby)
        {
            return await Save(cacheKey, lobby!, transaction);
        }
        else
        {
            return new UpdateEntityResult<Lobby>(lobby, UpdateEntityStatus.NoOperation);
        }
    }
}