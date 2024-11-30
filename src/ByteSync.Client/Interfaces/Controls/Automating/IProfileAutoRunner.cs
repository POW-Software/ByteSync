using System.Threading.Tasks;
using ByteSync.Common.Business.Lobbies;

namespace ByteSync.Interfaces.Controls.Automating;

public interface IProfileAutoRunner
{
    Task<int> OperateRunProfile(string? profileName, JoinLobbyModes? joinLobbyModes);
}