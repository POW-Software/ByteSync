using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;

namespace ByteSync.Interfaces.Updates;

public interface IApplyUpdateService
{
    Task<bool> Update(SoftwareVersion softwareVersion, SoftwareVersionFile softwareFileVersion, CancellationToken cancellationToken);
}