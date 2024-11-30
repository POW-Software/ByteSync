using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using PowSoftware.Common.Business.Versions;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateService
{
    public IObservableCache<SoftwareVersion, string> NextVersions { get; set; }
        
    Task SearchNextAvailableVersionsAsync();

    Task<bool> UpdateAsync(SoftwareVersion softwareVersion, CancellationToken cancellationToken);
}