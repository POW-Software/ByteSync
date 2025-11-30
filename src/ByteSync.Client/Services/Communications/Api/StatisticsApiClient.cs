using System.Threading.Tasks;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Api;

public class StatisticsApiClient : IStatisticsApiClient
{
    private readonly IApiInvoker _apiInvoker;
    private readonly ILogger<StatisticsApiClient> _logger;
    
    public StatisticsApiClient(IApiInvoker apiInvoker, ILogger<StatisticsApiClient> logger)
    {
        _apiInvoker = apiInvoker;
        _logger = logger;
    }
    
    public async Task<UsageStatisticsData> GetUsageStatistics(UsageStatisticsRequest usageStatisticsRequest)
    {
        var result = await _apiInvoker.PostAsync<UsageStatisticsData>("statistics/getUsageStatistics", usageStatisticsRequest);

        return result;
    }
}