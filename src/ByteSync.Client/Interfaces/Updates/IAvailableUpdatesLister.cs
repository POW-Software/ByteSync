using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;

namespace ByteSync.Interfaces.Updates;

public interface IAvailableUpdatesLister
{
    Task<List<SoftwareVersion>> GetAvailableUpdates();

    string GetUrl(SoftwareVersionFile softwareFileVersion);
}