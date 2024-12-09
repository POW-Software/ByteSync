using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;
using DynamicData;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateService
{
    public IObservableCache<SoftwareVersion, string> NextVersions { get; set; }
        
    Task SearchNextAvailableVersionsAsync();

    Task<bool> UpdateAsync(SoftwareVersion softwareVersion, CancellationToken cancellationToken);
}