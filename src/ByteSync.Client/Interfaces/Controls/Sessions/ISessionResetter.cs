using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Sessions;

public interface ISessionResetter
{
    public Task ResetSession();
}