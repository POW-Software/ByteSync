using System.Threading.Tasks;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Misc;

public class StatisticsService : IStatisticsService
{
    private readonly IStatisticsApiClient _statisticsApiClient;

    public StatisticsService(IStatisticsApiClient statisticsApiClient)
    {
        _statisticsApiClient = statisticsApiClient;
    }
    
    public async Task<UsageStatisticsData> GetUsageStatistics(UsageStatisticsRequest usageStatisticsRequest)
    {
        var result = await _statisticsApiClient.GetUsageStatistics(usageStatisticsRequest);

        return result;
    }
}