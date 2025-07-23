using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.DataNodes;

public class DataNodeProxy : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public DataNodeProxy(DataNode dataNode)
    {
        DataNode = dataNode;
        NodeId = dataNode.Id;
        
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
}
