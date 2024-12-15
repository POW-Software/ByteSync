using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateService
{
    Task SearchNextAvailableVersionsAsync();

    Task<bool> UpdateAsync(SoftwareVersion softwareVersion, CancellationToken cancellationToken);
}