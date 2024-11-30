using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Services;

public class UsageStatisticsService : IUsageStatisticsService
{
    public Task RegisterUploadUsage(Client client, SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        return Task.CompletedTask;
    }

    public Task SaveUploadUsage()
    {
        return Task.CompletedTask;
    }

    public Task<UsageStatisticsData> GetUsageStatistics(Client client, UsageStatisticsRequest usageStatisticsRequest)
    {
        return Task.FromResult(new UsageStatisticsData());
    }
}