using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Business.DataSources;
using ByteSync.Interfaces.Factories.Proxies;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.DataNodes;

public class DataNodeProxy : ReactiveObject, IDisposable
{
    private readonly IDataSourceProxyFactory _dataSourceProxyFactory;
    private readonly ReadOnlyObservableCollection<DataSourceProxy> _dataSources;
    
    private readonly CompositeDisposable _disposables = new();

    public DataNodeProxy(DataNode dataNode, IDataSourceProxyFactory dataSourceProxyFactory)
    {
        _dataSourceProxyFactory = dataSourceProxyFactory;
        DataNode = dataNode;
        NodeId = dataNode.NodeId;

        // var list = new ObservableCollection<DataSourceProxy>(
        //     dataNode.DataSources.Select(ds => _dataSourceProxyFactory.CreateDataSourceProxy(ds))
        // );
        // _dataSources = new ReadOnlyObservableCollection<DataSourceProxy>(list);
        
        var codeSubscription = DataNode
            .WhenAnyValue(x => x.Code)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(newCode => Code = newCode);
        _disposables.Add(codeSubscription);
    }

    public DataNode DataNode { get; }

    public string NodeId { get; }
    
    [Reactive]
    public string Code { get; set; } = null!;
    
    public void Dispose()
    {
        _disposables.Dispose();
    }

    // public ReadOnlyObservableCollection<DataSourceProxy> DataSources => _dataSources;
}
