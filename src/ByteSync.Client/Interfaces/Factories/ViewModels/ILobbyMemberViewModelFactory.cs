using ByteSync.Business.Lobbies;
using ByteSync.ViewModels.Lobbies;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface ILobbyMemberViewModelFactory
{
    LobbyMemberViewModel CreateLobbyMemberViewModel(LobbyMember lobbyMember);
}