using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class AvailableUpdateRepository : BaseSourceCacheRepository<SoftwareVersion, string>, IAvailableUpdateRepository
{
    protected override string KeySelector(SoftwareVersion softwareVersion) => softwareVersion.Version;

    public void UpdateAvailableUpdates(List<SoftwareVersion> nextAvailableVersions)
    {
        foreach (SoftwareVersion softwareVersion in nextAvailableVersions)
        {
            SourceCache.AddOrUpdate(softwareVersion);
        }

        var currentKeys = SourceCache.Keys.ToList();

        foreach (var key in currentKeys)
        {
            var itemInUpdateCollection = nextAvailableVersions.FirstOrDefault(item => item.Version.Equals(key));

            if (itemInUpdateCollection == null)
            {
                SourceCache.RemoveKey(key);
            }
        }
    }
}