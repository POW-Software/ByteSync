using System.Threading;
using System.Threading.Tasks;
using PowSoftware.Common.Business.Versions;

namespace ByteSync.Interfaces.Updates;

public interface IApplyUpdateService
{
    Task<bool> Update(SoftwareVersion softwareVersion, SoftwareVersionFile softwareFileVersion, CancellationToken cancellationToken);
}