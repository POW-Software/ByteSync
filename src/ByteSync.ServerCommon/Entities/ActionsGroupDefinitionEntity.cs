using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.ServerCommon.Entities;

public class ActionsGroupDefinitionEntity
{
    public string ActionsGroupDefinitionEntityId { get; set; } = null!;
    
    public string SessionId { get; set; } = null!;
    
    public string? Source { get; set; }
    
    public List<string> Targets { get; set; }

    public FileSystemTypes FileSystemType { get; set; }
    
    public ActionOperatorTypes Operator { get; set; }
    
    public long? Size { get; set; }
    
    public DateTime? CreationTimeUtc { get; set; }
    
    public bool AppliesOnlySynchronizeDate { get; set; }
    
    public DateTime? LastWriteTimeUtc { get; set; }
}