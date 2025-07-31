namespace ByteSync.Business.Communications.Transfers;

public class UploadProgressState
{
    public int TotalCreatedSlices { get; set; }
    public int TotalUploadedSlices { get; set; }
    public int ConcurrentUploads { get; set; }
    public int MaxConcurrentUploads { get; set; }
    public Exception? LastException { get; set; }
} 