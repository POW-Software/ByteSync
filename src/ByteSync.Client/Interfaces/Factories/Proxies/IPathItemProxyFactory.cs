using ByteSync.Business.PathItems;

namespace ByteSync.Interfaces.Factories.Proxies;

public interface IPathItemProxyFactory
{
    PathItemProxy CreatePathItemProxy(PathItem pathItem);
}