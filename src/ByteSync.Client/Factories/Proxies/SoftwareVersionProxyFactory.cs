using Autofac;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.ViewModels.Headers;

namespace ByteSync.Factories.Proxies;

public class SoftwareVersionProxyFactory : ISoftwareVersionProxyFactory
{
    private readonly IComponentContext _context;

    public SoftwareVersionProxyFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public SoftwareVersionProxy CreateSoftwareVersionProxy(SoftwareVersion softwareVersion)
    {
        var result = _context.Resolve<SoftwareVersionProxy>(
            new TypedParameter(typeof(SoftwareVersion), softwareVersion));

        return result;
    }
}