using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Sessions;

public interface ISessionInterruptor
{
    Task RequestQuitSession();
    
    Task RequestRestartSession();
}