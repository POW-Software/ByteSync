namespace ByteSync.Business.Communications.Transfers;

public class SliceUploadMetric
{
    public int PartNumber { get; set; }
    public long Bytes { get; set; }
    public long DurationMs { get; set; }
    public double BandwidthKbps { get; set; }
}


