using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;

namespace ByteSync.ServerCommon.Factories;

public class LobbyFactory : ILobbyFactory
{
    public Lobby BuildLobby(CloudSessionProfileEntity cloudSessionProfileData)
    {
        Lobby lobby = new Lobby();
        
        lobby.LobbyId = $"LobbyID_{Guid.NewGuid()}";
        
        lobby.CloudSessionProfileId = cloudSessionProfileData.CloudSessionProfileId;

        foreach (var slot in cloudSessionProfileData.Slots)
        {
            lobby.LobbyMemberCells.Add(new LobbyMemberCell(slot.ProfileClientId));
        }
        
        lobby.ProfileDetailsPassword = cloudSessionProfileData.ProfileDetailsPassword;

        return lobby;
    }
}