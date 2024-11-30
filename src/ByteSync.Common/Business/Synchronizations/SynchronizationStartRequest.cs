using System.Collections.Generic;
using ByteSync.Common.Business.Actions;

namespace ByteSync.Common.Business.Synchronizations;

public class SynchronizationStartRequest
{
    public string SessionId { get; set; } = null!;
    
    public List<ActionsGroupDefinition> ActionsGroupDefinitions { get; set; } = null!;
}