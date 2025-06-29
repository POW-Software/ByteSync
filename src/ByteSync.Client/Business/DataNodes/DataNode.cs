using ByteSync.Business.DataSources;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.DataNodes;

public class DataNode
{
    public DataNode()
    {
        DataSources = new List<DataSource>();
    }

    public string NodeId { get; set; } = null!;

    public string ClientInstanceId { get; set; } = null!;

    public List<DataSource> DataSources { get; set; }
    
    [Reactive]
    public string Code { get; set; }
}
