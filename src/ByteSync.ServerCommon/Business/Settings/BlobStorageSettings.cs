namespace ByteSync.ServerCommon.Business.Settings;

public class BlobStorageSettings
{
    public string AccountName { get; set; } = null!;
    
    public string AccountKey { get; set; } = null!;
    
    public string Endpoint { get; set; } = null!;
    
    public string Container { get; set; } = null!;

    public int RetentionDurationInDays { get; set; }
}