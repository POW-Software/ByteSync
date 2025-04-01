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
            // On utilise '/' comme séparateur, que ce soit sur Windows, Linux ou Mac
            // Car
            //  - '/' est le séparateur sur Linux et Mac
            //  ' "\" est le séparateu sur Windows
            // Et que '/' est interdit sur Windows dans les noms de fichiers
            // Alors que "\" est autorisé au moins sur Linux
            return RelativePath.Substring(RelativePath.LastIndexOf("/", StringComparison.Ordinal) + 1);
        }
    }
}