using ByteSync.Business.DataSources;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IDataSourceCodeGenerator
{
    void RecomputeCodesForNode(string dataNodeId);
}
