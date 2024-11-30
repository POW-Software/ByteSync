namespace ByteSync.Common.Business.Misc;

public class UsageStatisticsSubPeriod
{
    public UsageStatisticsSubPeriod()
    {
        
    }
    
    public UsageStatisticsSubPeriod(string name, long uploadedVolume)
    {
        Name = name;
        UploadedVolume = uploadedVolume;
    }

    public string Name { get; set; } = null!;

    public long UploadedVolume { get; set; }
}