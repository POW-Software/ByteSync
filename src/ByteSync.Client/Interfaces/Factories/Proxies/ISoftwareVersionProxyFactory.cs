using ByteSync.Common.Business.Versions;
using ByteSync.ViewModels.Headers;

namespace ByteSync.Interfaces.Factories.Proxies;

public interface ISoftwareVersionProxyFactory
{
    SoftwareVersionProxy CreateSoftwareVersionProxy(SoftwareVersion softwareVersion);
}