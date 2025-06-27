using Autofac;
using ByteSync.Business.DataSources;
using ByteSync.Interfaces.Factories.Proxies;

namespace ByteSync.Factories.Proxies;

public class DataSourceProxyFactory : IDataSourceProxyFactory
{
    private readonly IComponentContext _context;

    public DataSourceProxyFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public DataSourceProxy CreatePathItemProxy(DataSource dataSource)
    {
        var result = _context.Resolve<DataSourceProxy>(
            new TypedParameter(typeof(DataSource), dataSource));

        return result;
    }
}