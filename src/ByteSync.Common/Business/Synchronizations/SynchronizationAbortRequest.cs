using System;
using System.Collections.Generic;

namespace ByteSync.Common.Business.Synchronizations;

public class SynchronizationAbortRequest
{
    public string SessionId { get; set; } = null!;

    public DateTimeOffset RequestedOn { get; set; }

    public List<string> RequestedBy { get; set; } = new();
}