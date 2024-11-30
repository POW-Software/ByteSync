using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class LobbyRepository : BaseRepository<Lobby>, ILobbyRepository
{
    public LobbyRepository(ICacheService cacheService) : base(cacheService)
    {
    }
    
    public override string ElementName { get; } = "Lobby";

    public async Task<UpdateEntityResult<Lobby>> QuitLobby(string lobbyId, string clientInstanceId)
    {
        var database = _cacheService.GetDatabase();

        var cacheKey = ComputeCacheKey(ElementName, lobbyId);
        await using var redisLock = await _cacheService.AcquireLockAsync(cacheKey);

        var lobby = await GetCachedElement(cacheKey);

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
            await database.KeyDeleteAsync(cacheKey);

            return new UpdateEntityResult<Lobby>(lobby, UpdateEntityStatus.Deleted);
        }
        else if (updateLobby)
        {
            return await SetElement(cacheKey, lobby!, database);
        }
        else
        {
            return new UpdateEntityResult<Lobby>(lobby, UpdateEntityStatus.NoOperation);
        }
    }
}