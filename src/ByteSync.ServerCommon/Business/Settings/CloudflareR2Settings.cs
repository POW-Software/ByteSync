namespace ByteSync.ServerCommon.Business.Settings;

public class CloudflareR2Settings
{
    public string AccessKeyId { get; set; } = null!;
    
    public string SecretAccessKey { get; set; } = null!;
    
    public string Endpoint { get; set; } = null!;
    
    public string BucketName { get; set; } = null!;

    public int RetentionDurationInDays { get; set; }
} 