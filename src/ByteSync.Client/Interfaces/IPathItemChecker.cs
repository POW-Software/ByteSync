using System.Threading.Tasks;
using ByteSync.Business.DataSources;

namespace ByteSync.Interfaces;

public interface IPathItemChecker
{
    Task<bool> CheckPathItem(DataSource dataSource, IEnumerable<DataSource> existingDataSources);
}