using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Inventories;

namespace ByteSync.Models.FileSystems;

public class DirectoryDescription : FileSystemDescription
{
    public DirectoryDescription()
    {

    }

    public DirectoryDescription(InventoryPart inventoryPart, string relativePath)
        : base(inventoryPart, relativePath)
    {

    }

    public override FileSystemTypes FileSystemType
    {
        get
        {
            return FileSystemTypes.Directory;
        }
    }

    public bool IsAccessible { get; set; } = true;
}