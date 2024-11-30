using System;
using System.Collections.Generic;

namespace ByteSync.Common.Business.Synchronizations;

public class Synchronization
{
    public string SessionId { get; set; } = null!;
    public DateTimeOffset Started { get; set; }
    public string StartedBy { get; set; } = null!;
    
    public bool IsFatalError { get; set; }
    
    public DateTimeOffset? Ended { get; set; }
    
    public bool IsEnded => Ended.HasValue;
    
    public SynchronizationEndStatuses? EndStatus { get; set; }
    
    public DateTimeOffset? AbortRequestedOn { get; set; }
    
    public bool IsAbortRequested => AbortRequestedOn.HasValue;
    
    public List<string> AbortRequestedBy { get; set; } = new();
}