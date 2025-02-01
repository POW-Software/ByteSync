using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Sessions;

public interface IResetSessionService
{
    public Task ResetSession();
}