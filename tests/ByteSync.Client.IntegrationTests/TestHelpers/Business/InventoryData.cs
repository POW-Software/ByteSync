using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;

namespace ByteSync.Client.IntegrationTests.TestHelpers.Business;

public class InventoryData
{
    public InventoryData(DirectoryInfo inventoryPartRoot)
    {
        PathItems = new List<PathItem>();

        PathItem pathItem = new PathItem();
        pathItem.Path = inventoryPartRoot.FullName;
        pathItem.Type = FileSystemTypes.Directory;
        
        PathItems.Add(pathItem);
    }
    
    public InventoryData(params DirectoryInfo[] inventoryPartRoots)
    {
        PathItems = new List<PathItem>();

        foreach (var inventoryPartRoot in inventoryPartRoots)
        {
            PathItem pathItem = new PathItem();
            pathItem.Path = inventoryPartRoot.FullName;
            pathItem.Type = FileSystemTypes.Directory;
        
            PathItems.Add(pathItem);
        }
    }
    
    public InventoryData(FileInfo inventoryPartRoot)
    {
        PathItems = new List<PathItem>();

        PathItem pathItem = new PathItem();
        pathItem.Path = inventoryPartRoot.FullName;
        pathItem.Type = FileSystemTypes.File;
        
        PathItems.Add(pathItem);
    }

    public List<PathItem> PathItems { get; }

    public string Letter { get; private set; }
    
    public InventoryBuilder InventoryBuilder { get; set; }

    internal Inventory Inventory
    {
        get
        {
            return InventoryBuilder.Inventory;
        }
    }

    internal List<InventoryPart> InventoryParts
    {
        get
        {
            return Inventory.InventoryParts;
        }
    }

    internal List<FileDescription> AllFileDescriptions
    {
        get
        {
            List<FileDescription> fileDescriptions = new List<FileDescription>();

            foreach (var inventoryPart in InventoryParts)
            {
                fileDescriptions.AddAll(inventoryPart.FileDescriptions);
            }

            return fileDescriptions;
        }
    }

    public void SetLetter(string letter)
    {
        Letter = letter;

        int cpt = 0;
        foreach (var pathItem in PathItems)
        {
            cpt += 1;
            
            string code = letter + cpt;
            pathItem.Code = code;
        }
    }
}