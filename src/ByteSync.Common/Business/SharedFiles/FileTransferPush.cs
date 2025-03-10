﻿using System.Collections.Generic;

namespace ByteSync.Common.Business.SharedFiles;

public class FileTransferPush
{
    public string SessionId { get; set; } = null!;
    
    public SharedFileDefinition SharedFileDefinition { get; set; } = null!;
    
    public int? PartNumber{ get; set; } 
    
    public int? TotalParts { get; set; }
    
    public List<string>? ActionsGroupIds { get; set; }
}