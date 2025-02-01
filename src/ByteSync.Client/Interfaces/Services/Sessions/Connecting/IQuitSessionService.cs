using System.Threading.Tasks;

namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface IQuitSessionService
{
    Task Process();
}