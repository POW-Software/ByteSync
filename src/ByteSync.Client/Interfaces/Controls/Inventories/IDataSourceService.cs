using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IDataSourceService
{
    Task<bool> TryAddDataSource(DataSource dataSource, string? nodeId = null);

    Task CreateAndTryAddDataSource(string path, FileSystemTypes fileSystemType, string? nodeId = null);

    void ApplyAddDataSourceLocally(DataSource dataSource);

    Task<bool> TryRemoveDataSource(DataSource dataSource, string? nodeId = null);

    void ApplyRemoveDataSourceLocally(DataSource dataSource);
}