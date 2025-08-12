using Autofac;
using ByteSync.Business.DataNodes;
using ByteSync.Interfaces.Factories.Proxies;

namespace ByteSync.Factories.Proxies;

public class DataNodeProxyFactory : IDataNodeProxyFactory
{
    private readonly IComponentContext _context;

    public DataNodeProxyFactory(IComponentContext context)
    {
        _context = context;
    }

    public DataNodeProxy CreateDataNodeProxy(DataNode dataNode)
    {
        return _context.Resolve<DataNodeProxy>(new TypedParameter(typeof(DataNode), dataNode));
    }
}
