using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IUsageStatisticsService
{
    Task RegisterUploadUsage(Client client, SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task SaveUploadUsage();
    
    Task<UsageStatisticsData> GetUsageStatistics(Client client, UsageStatisticsRequest usageStatisticsRequest);
}