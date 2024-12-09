using ByteSync.Common.Business.Versions;

namespace ByteSync.Interfaces.Updates;

public interface IAvailableUpdatesLister
{
    List<SoftwareVersion> GetAvailableUpdates();

    string GetUrl(SoftwareVersionFile softwareFileVersion);
}