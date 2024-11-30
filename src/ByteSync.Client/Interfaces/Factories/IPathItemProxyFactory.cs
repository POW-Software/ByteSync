using ByteSync.Business.PathItems;

namespace ByteSync.Interfaces.Factories;

public interface IPathItemProxyFactory
{
    PathItemProxy CreatePathItemProxy(PathItem pathItem);
}