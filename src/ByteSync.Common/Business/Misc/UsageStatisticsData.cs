using System;

namespace ByteSync.Common.Business.Misc;

public class UsageStatisticsData
{
    public UsageStatisticsData()
    {

    }
    
    public UsageStatisticsData(UsageStatisticsPeriod currentPeriodData, UsageStatisticsPeriod previousPeriodData)
    {
        CurrentPeriodData = currentPeriodData;
        PreviousPeriodData = previousPeriodData;
    }
    
    public UsageStatisticsPeriod CurrentPeriodData { get; set; } = null!;

    public UsageStatisticsPeriod PreviousPeriodData { get; set; } = null!;

    public long GetMaxTransferedVolume()
    {
        return Math.Max(CurrentPeriodData.GetMaxUploadedVolume(), PreviousPeriodData.GetMaxUploadedVolume());
    }
}