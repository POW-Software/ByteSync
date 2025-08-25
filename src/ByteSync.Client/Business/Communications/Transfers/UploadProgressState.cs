namespace ByteSync.Business.Communications.Transfers;

public class UploadProgressState
{
    public int TotalCreatedSlices { get; set; }
    public int TotalUploadedSlices { get; set; }
    public int ConcurrentUploads { get; set; }
    public int MaxConcurrentUploads { get; set; }
    public DateTimeOffset? StartTimeUtc { get; set; }
    public DateTimeOffset? EndTimeUtc { get; set; }
    public long TotalCreatedBytes { get; set; }
    public long TotalUploadedBytes { get; set; }
    public long? LastSliceUploadedBytes { get; set; }
    public long? LastSliceUploadDurationMs { get; set; }
    public List<Exception> Exceptions { get; } = new System.Collections.Generic.List<Exception>();
    public List<SliceUploadMetric> SliceMetrics { get; } = new System.Collections.Generic.List<SliceUploadMetric>();
} 