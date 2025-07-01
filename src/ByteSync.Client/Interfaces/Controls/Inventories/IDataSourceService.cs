using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IDataSourceService
{
    Task<bool> TryAddDataSource(DataSource dataSource);

    Task CreateAndTryAddDataSource(string path, FileSystemTypes fileSystemType, DataNode dataNode);

    void ApplyAddDataSourceLocally(DataSource dataSource);

    Task<bool> TryRemoveDataSource(DataSource dataSource);

    void ApplyRemoveDataSourceLocally(DataSource dataSource);
}