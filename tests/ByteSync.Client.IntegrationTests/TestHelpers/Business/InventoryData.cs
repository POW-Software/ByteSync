using ByteSync.Business.DataSources;
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
        DataSources = new List<DataSource>();

        DataSource dataSource = new DataSource();
        dataSource.Path = inventoryPartRoot.FullName;
        dataSource.Type = FileSystemTypes.Directory;
        
        DataSources.Add(dataSource);
    }
    
    public InventoryData(params DirectoryInfo[] inventoryPartRoots)
    {
        DataSources = new List<DataSource>();

        foreach (var inventoryPartRoot in inventoryPartRoots)
        {
            DataSource dataSource = new DataSource();
            dataSource.Path = inventoryPartRoot.FullName;
            dataSource.Type = FileSystemTypes.Directory;
        
            DataSources.Add(dataSource);
        }
    }
    
    public InventoryData(FileInfo inventoryPartRoot)
    {
        DataSources = new List<DataSource>();

        DataSource dataSource = new DataSource();
        dataSource.Path = inventoryPartRoot.FullName;
        dataSource.Type = FileSystemTypes.File;
        
        DataSources.Add(dataSource);
    }

    public List<DataSource> DataSources { get; }

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
        foreach (var pathItem in DataSources)
        {
            cpt += 1;
            
            string code = letter + cpt;
            pathItem.Code = code;
        }
    }
}