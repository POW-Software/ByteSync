using Autofac;
using ByteSync.Business.PathItems;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Factories;

public class PathItemProxyFactory : IPathItemProxyFactory
{
    private readonly IComponentContext _context;

    public PathItemProxyFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public PathItemProxy CreatePathItemProxy(PathItem pathItem)
    {
        var result = _context.Resolve<PathItemProxy>(
            new TypedParameter(typeof(PathItem), pathItem));

        return result;
    }
}