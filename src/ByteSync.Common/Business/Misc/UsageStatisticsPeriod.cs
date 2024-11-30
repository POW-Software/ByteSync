using System.Collections.Generic;
using System.Linq;

namespace ByteSync.Common.Business.Misc;

public class UsageStatisticsPeriod
{
    public UsageStatisticsPeriod()
    {
        UploadedVolumePerSubPeriod = new List<UsageStatisticsSubPeriod>();
    }
    
    public string Name { get; set; }
    
    public List<UsageStatisticsSubPeriod> UploadedVolumePerSubPeriod { get; set; }

    public long GetMaxUploadedVolume()
    {
        return UploadedVolumePerSubPeriod.Max(v => v.UploadedVolume);
    }
}