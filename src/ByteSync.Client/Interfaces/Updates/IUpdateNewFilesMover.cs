using System.Threading;
using System.Threading.Tasks;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateNewFilesMover
{
    Task MoveNewFiles(CancellationToken cancellationToken);
}