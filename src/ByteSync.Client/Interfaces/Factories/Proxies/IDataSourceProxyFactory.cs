using ByteSync.Business.DataSources;

namespace ByteSync.Interfaces.Factories.Proxies;

public interface IDataSourceProxyFactory
{
    DataSourceProxy CreateDataSourceProxy(DataSource dataSource);
}