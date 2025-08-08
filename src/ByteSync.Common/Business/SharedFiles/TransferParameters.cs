using System.Collections.Generic;

namespace ByteSync.Common.Business.SharedFiles;

public class TransferParameters
{
    public string SessionId { get; set; } = null!;

    public SharedFileDefinition SharedFileDefinition { get; set; } = null!;

    public int? PartNumber { get; set; }
    
    public int? TotalParts { get; set; }
    
    public List<string>? ActionsGroupIds { get; set; }
    
    public StorageProvider StorageProvider { get; set; }
}