using System.Threading.Tasks;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface IStatisticsApiClient
{
    Task<UsageStatisticsData> GetUsageStatistics(UsageStatisticsRequest usageStatisticsRequest);
}