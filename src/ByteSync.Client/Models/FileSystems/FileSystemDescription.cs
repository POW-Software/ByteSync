using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Inventories;

namespace ByteSync.Models.FileSystems;

public abstract class FileSystemDescription
{
    protected FileSystemDescription()
    {

    }

    protected FileSystemDescription(InventoryPart inventoryPart, string relativePath)
    {
        InventoryPart = inventoryPart;
        RelativePath = relativePath;
    }

    public InventoryPart InventoryPart { get; set; }

    public string RelativePath { get; set; }

    // Indicates if this item was accessible during inventory identification
    public bool IsAccessible { get; set; } = true;

    public abstract FileSystemTypes FileSystemType { get; }

    public Inventory Inventory
    {
        get
        {
            return InventoryPart.Inventory;
        }
    }

    public string Name
    {
        get
        {
            // We use '/' as a separator, whether on Windows, Linux or Mac
            // Because
            // - '/' is the separator on Linux and Mac
            // ' "\" is the separator on Windows
            // And that '/' is forbidden on Windows in file names
            // While "\" is allowed at least on Linux
            return RelativePath.Substring(RelativePath.LastIndexOf("/", StringComparison.Ordinal) + 1);
        }
    }
}