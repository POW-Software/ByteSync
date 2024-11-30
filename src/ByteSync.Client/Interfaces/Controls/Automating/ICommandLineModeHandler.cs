using System.Threading.Tasks;
using ByteSync.Common.Business.Lobbies;

namespace ByteSync.Interfaces.Controls.Automating;

public interface ICommandLineModeHandler
{
    public Task<int> Operate();
    

    
    string? ProfileToRunName { get; }
    
    JoinLobbyModes? JoinLobbyMode { get; }
}