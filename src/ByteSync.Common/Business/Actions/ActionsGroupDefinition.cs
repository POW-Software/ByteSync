using System.Collections.Generic;
using ByteSync.Common.Business.Inventories;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
#pragma warning disable CS8618

namespace ByteSync.Common.Business.Actions;

public class ActionsGroupDefinition : AbstractActionsGroup
{
    public ActionsGroupDefinition()
    {
        TargetClientInstanceAndNodeIds = new List<string>();
    }

    public string? SourceClientInstanceId { get; set; }
    
    public List<string> TargetClientInstanceAndNodeIds { get; set; }

    public FileSystemTypes FileSystemType { get; set; }
}
