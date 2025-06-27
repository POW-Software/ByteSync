using System.Threading.Tasks;
using ByteSync.Business.DataSources;

namespace ByteSync.Interfaces;

public interface IDataSourceChecker
{
    Task<bool> CheckDataSource(DataSource dataSource, IEnumerable<DataSource> existingDataSources);
}