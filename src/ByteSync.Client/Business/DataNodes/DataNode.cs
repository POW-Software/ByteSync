using ByteSync.Business.DataSources;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.DataNodes;

public class DataNode
{
    public DataNode()
    {

    }

    public string NodeId { get; set; } = null!;

    public string ClientInstanceId { get; set; } = null!;
    
    [Reactive]
    public string Code { get; set; }
    
    [Reactive]
    public int OrderIndex { get; set; }
}
