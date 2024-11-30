using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.ServerCommon.Entities;

public class SynchronizationEntity
{
    public string SessionId { get; set; } = null!;
    
    public bool IsFatalError { get; set; }
    
    public DateTimeOffset StartedOn { get; set; }

    public string StartedBy { get; set; } = null!;
    
    public DateTimeOffset? EndedOn { get; set; }
    
    public SynchronizationEndStatuses? EndStatus { get; set; }
    
    public DateTimeOffset? AbortRequestedOn { get; set; }
    
    public List<string> AbortRequestedBy { get; set; } = new();
    
    public SynchronizationProgressEntity Progress { get; set; } = new();
    
    public bool IsEnded => EndedOn.HasValue;
    
    public bool IsAbortRequested => AbortRequestedOn.HasValue;
}