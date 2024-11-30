using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Factories;

public interface ILobbyFactory
{
    Lobby BuildLobby(CloudSessionProfileEntity cloudSessionProfileData);
}