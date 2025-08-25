namespace ByteSync.Business.Communications.Transfers;

public class SliceUploadMetric
{
    public int TaskId { get; set; }

    public int PartNumber { get; set; }
    
    public long Bytes { get; set; }
    
    public long ElapsedtimeMs { get; set; }

}


