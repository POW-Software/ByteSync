using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;

namespace ByteSync.Interfaces.Controls.Actions;

public interface ISharedAtomicActionComputer
{
    Task<List<SharedAtomicAction>> ComputeSharedAtomicActions();
}