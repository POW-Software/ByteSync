using System.Threading.Tasks;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces;

public interface IStatisticsService
{
    Task<UsageStatisticsData> GetUsageStatistics(UsageStatisticsRequest usageStatisticsRequest);
}