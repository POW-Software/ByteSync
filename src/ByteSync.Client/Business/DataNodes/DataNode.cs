using ByteSync.Business.DataSources;

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
}
