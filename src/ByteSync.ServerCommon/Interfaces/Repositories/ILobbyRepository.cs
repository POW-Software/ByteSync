using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface ILobbyRepository : IRepository<Lobby>
{
    Task<UpdateEntityResult<Lobby>> QuitLobby(string lobbyId, string clientInstanceId, ITransaction transaction);
}