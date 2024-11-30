using System;

namespace ByteSync.Common.Business.Synchronizations;

public enum SynchronizationEndStatuses { 
    Regular = 1,
    Abortion = 2, 
    Error = 3,
    Undefined4 = 4,
    Undefined5 = 5,
    Undefined6 = 6,
}

public class SynchronizationEnd
{
    public string SessionId { get; set; }
    
    public DateTimeOffset FinishedOn { get; set; }
    
    public SynchronizationEndStatuses Status { get; set; }
}