using Autofac;
using ByteSync.Business.Lobbies;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Lobbies;

namespace ByteSync.Factories.ViewModels;

public class LobbyMemberViewModelFactory : ILobbyMemberViewModelFactory
{
    private readonly IComponentContext _context;

    public LobbyMemberViewModelFactory(IComponentContext context)
    {
        _context = context;
    }

    public LobbyMemberViewModel CreateLobbyMemberViewModel(LobbyMember lobbyMember)
    {
        var result = _context.Resolve<LobbyMemberViewModel>(
            new TypedParameter(typeof(LobbyMember), lobbyMember));

        return result;
    }
}