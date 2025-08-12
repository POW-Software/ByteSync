using ByteSync.Business.DataNodes;

namespace ByteSync.Interfaces.Factories.Proxies;

public interface IDataNodeProxyFactory
{
    DataNodeProxy CreateDataNodeProxy(DataNode dataNode);
}
