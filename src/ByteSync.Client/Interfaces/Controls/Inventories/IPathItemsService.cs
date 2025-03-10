﻿using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IPathItemsService
{
    Task<bool> TryAddPathItem(PathItem pathItem);
    
    Task CreateAndTryAddPathItem(string path, FileSystemTypes fileSystemType);

    public void ApplyAddPathItemLocally(PathItem pathItem);
    
    Task<bool> TryRemovePathItem(PathItem pathItem);

    public void ApplyRemovePathItemLocally(PathItem pathItem);
}