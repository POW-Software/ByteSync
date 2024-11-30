namespace ByteSync.Common.Business.Serials;

public class ProductSerialDescription
{
    public ProductSerialDescription()
    {

    }
    
    public string Email { get; set; }

    public string SerialNumber { get; set; }

    public string ProductName { get; set; }

    public string Subscription { get; set; }
        
    public long AllowedCloudSynchronizationVolumeInBytes { get; set; }
    
    public SerialStatus Status { get; set; }
}