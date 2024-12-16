using ByteSync.Common.Business.Versions;

namespace ByteSync.Interfaces.Repositories;

public interface IAvailableUpdateRepository : IBaseSourceCacheRepository<SoftwareVersion, string>
{
    public void UpdateAvailableUpdates(List<SoftwareVersion> availableUpdates);
}