using System.Collections.ObjectModel;
using System.Linq;
using ByteSync.Business.DataSources;
using ByteSync.Interfaces.Factories.Proxies;

namespace ByteSync.Business.DataNodes;

public class DataNodeProxy
{
    private readonly IDataSourceProxyFactory _dataSourceProxyFactory;
    private readonly ReadOnlyObservableCollection<DataSourceProxy> _dataSources;

    public DataNodeProxy(DataNode dataNode, IDataSourceProxyFactory dataSourceProxyFactory)
    {
        _dataSourceProxyFactory = dataSourceProxyFactory;
        DataNode = dataNode;
        NodeId = dataNode.NodeId;

        var list = new ObservableCollection<DataSourceProxy>(
            dataNode.DataSources.Select(ds => _dataSourceProxyFactory.CreateDataSourceProxy(ds))
        );
        _dataSources = new ReadOnlyObservableCollection<DataSourceProxy>(list);
    }

    public DataNode DataNode { get; }

    public string NodeId { get; }

    public ReadOnlyObservableCollection<DataSourceProxy> DataSources => _dataSources;
}
