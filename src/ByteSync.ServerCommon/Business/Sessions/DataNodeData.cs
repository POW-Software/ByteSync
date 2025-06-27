using ByteSync.Common.Business.Inventories;

namespace ByteSync.ServerCommon.Business.Sessions;

public class DataNodeData
{
    public DataNodeData()
    {
        DataSources = new List<EncryptedDataSource>();
    }

    public string NodeId { get; set; } = null!;

    public List<EncryptedDataSource> DataSources { get; set; }
}

