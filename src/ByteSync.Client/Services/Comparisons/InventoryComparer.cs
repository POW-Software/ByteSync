using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;

namespace ByteSync.Services.Comparisons;

public class InventoryComparer : IInventoryComparer
{
    public InventoryComparer(SessionSettings sessionSettings, InventoryIndexer? inventoryIndexer = null)
    {
        SessionSettings = sessionSettings;
        Indexer = inventoryIndexer;
            
        InventoryLoaders = new List<InventoryLoader>();
        ComparisonResult = new ComparisonResult();
    }

    public void AddInventory(string inventoryFullName)
    {
        if (InventoryLoaders.Any(il => il.FullName.Equals(inventoryFullName, StringComparison.InvariantCultureIgnoreCase)))
        {
            throw new ArgumentOutOfRangeException(nameof(inventoryFullName), "Already having inventory with same path");
        }
            
        var inventoryLoader = new InventoryLoader(inventoryFullName);
        InventoryLoaders.Add(inventoryLoader);
    }
        
    public void AddInventories(ICollection<InventoryFile> inventoriesFiles)
    {
        foreach (var inventoryFile in inventoriesFiles)
        {
            AddInventory(inventoryFile.FullName);
        }
    }

    private List<InventoryLoader> InventoryLoaders { get; set; }
        
    public SessionSettings SessionSettings { get; set; }

    private ComparisonResult ComparisonResult { get; set; } 

    public InventoryIndexer? Indexer { get; set; }

    public ComparisonResult Compare()
    {
        ComparisonResult.Clear();

        foreach (var inventoryLoader in InventoryLoaders.OrderBy(il => il.Inventory.Letter))
        {
            var inventory = inventoryLoader.Inventory;
            ComparisonResult.AddInventory(inventory);

            foreach (var inventoryPart in inventory.InventoryParts)
            {
                if (SessionSettings.DataType.In(DataTypes.Files, DataTypes.FilesDirectories))
                {
                    foreach (var fileDescription in inventoryPart.FileDescriptions)
                    {
                        HandleFileDescription(inventoryLoader, fileDescription);
                    }
                }

                if (SessionSettings.DataType.In(DataTypes.Directories, DataTypes.FilesDirectories))
                {
                    foreach (var directoryDescription in inventoryPart.DirectoryDescriptions)
                    {
                        HandleDirectoryDescription(inventoryLoader, directoryDescription);
                    }
                }
            }
        }

        foreach (var comparisonItem in ComparisonResult.ComparisonItems)
        {
            BuildStatus(comparisonItem);
        }

        return ComparisonResult;
    }

    private void HandleFileDescription(InventoryLoader inventoryLoader, FileDescription fileDescription)
    {
        var contentIdentityCore = BuildContentIdentityCore(inventoryLoader, fileDescription);

        var pathIdentity = BuildPathIdentity(fileDescription);
        Indexer?.Register(fileDescription, pathIdentity);

        var comparisonItem = ComparisonResult.GetItemBy(pathIdentity);

        if (comparisonItem == null)
        {
            comparisonItem = new ComparisonItem(pathIdentity);
            ComparisonResult.AddItem(comparisonItem);
        }

        ContentIdentity? contentIdentity = null;
        if (!fileDescription.HasAnalysisError)
        {
            contentIdentity = comparisonItem.GetContentIdentity(contentIdentityCore);
        }
        if (contentIdentity == null)
        {
            contentIdentity = new ContentIdentity(contentIdentityCore);
            comparisonItem.AddContentIdentity(contentIdentity);
        }

        contentIdentity.Add(fileDescription);
    }
        
    private void HandleDirectoryDescription(InventoryLoader inventoryLoader, DirectoryDescription directoryDescription)
    {
        var pathIdentity = BuildPathIdentity(directoryDescription);
        Indexer?.Register(directoryDescription, pathIdentity);
            
        var comparisonItem = ComparisonResult.GetItemBy(pathIdentity);

        ContentIdentity contentIdentity;
        if (comparisonItem == null)
        {
            comparisonItem = new ComparisonItem(pathIdentity);
                
            contentIdentity = new ContentIdentity(null);
            comparisonItem.AddContentIdentity(contentIdentity);
                
            ComparisonResult.AddItem(comparisonItem);
        }
        else
        {
            contentIdentity = comparisonItem.ContentIdentities.Single();
        }

        contentIdentity.Add(directoryDescription);
    }
        
    private PathIdentity BuildPathIdentity(FileSystemDescription fileSystemDescription)
    {
        string linkingData;
        if (SessionSettings.LinkingKey == LinkingKeys.RelativePath)
        {
            if (SessionSettings.LinkingCase == LinkingCases.Sensitive)
            {
                linkingData = fileSystemDescription.RelativePath;
            }
            else if (SessionSettings.LinkingCase == LinkingCases.Insensitive)
            {
                linkingData = fileSystemDescription.RelativePath.ToLower();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(SessionSettings.LinkingCase));
            }
        }
        else if (SessionSettings.LinkingKey == LinkingKeys.Name)
        {
            if (SessionSettings.LinkingCase == LinkingCases.Sensitive)
            {
                linkingData = fileSystemDescription.Name;
            }
            else if (SessionSettings.LinkingCase == LinkingCases.Insensitive)
            {
                linkingData = fileSystemDescription.Name.ToLower();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(SessionSettings.LinkingCase));
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(SessionSettings.LinkingKey));
        }

        string linkingKeyValue;
        if (SessionSettings.LinkingKey == LinkingKeys.RelativePath)
        {
            linkingKeyValue = fileSystemDescription.RelativePath;
        }
        else
        {
            linkingKeyValue = fileSystemDescription.Name;
        }

        FileSystemTypes type;
        if (fileSystemDescription is FileDescription)
        {
            type = FileSystemTypes.File;
        }
        else if (fileSystemDescription is DirectoryDescription)
        {
            type = FileSystemTypes.Directory;
        }
        else
        {
            throw new ApplicationException("unknown type");
        }
            
        var pathIdentity = new PathIdentity(type, linkingKeyValue, 
            fileSystemDescription.Name, linkingData);

        return pathIdentity;
    }

    private void BuildStatus(ComparisonItem comparisonItem)
    {
        HashSet<Inventory> nonfoundInventories = new HashSet<Inventory>();
        HashSet<InventoryPart> nonfoundInventoryParts = new HashSet<InventoryPart>();
        foreach (var inventoryLoader in InventoryLoaders.OrderBy(il => il.Inventory.Letter))
        {
            nonfoundInventories.Add(inventoryLoader.Inventory);
            foreach (var inventoryPart in inventoryLoader.Inventory.InventoryParts)
            {
                nonfoundInventoryParts.Add(inventoryPart);
            }
        }

        if (comparisonItem.FileSystemType == FileSystemTypes.File)
        {
            foreach (var contentIdentity in comparisonItem.ContentIdentities)
            {
                if (comparisonItem.FileSystemType == FileSystemTypes.File)
                {
                    if (!comparisonItem.Status.FingerPrintGroups.ContainsKey(contentIdentity.Core))
                    {
                        comparisonItem.Status.FingerPrintGroups.Add(contentIdentity.Core, new HashSet<InventoryPart>());
                    }
                }

                foreach (KeyValuePair<DateTime, HashSet<InventoryPart>> pair in contentIdentity.InventoryPartsByLastWriteTimes)
                {
                    comparisonItem.Status.FingerPrintGroups[contentIdentity.Core].AddAll(pair.Value);

                    foreach (var inventoryPart in pair.Value)
                    {
                        nonfoundInventories.Remove(inventoryPart.Inventory);
                        nonfoundInventoryParts.Remove(inventoryPart);
                    }
                }


                foreach (var pair in contentIdentity.InventoryPartsByLastWriteTimes)
                {
                    if (!comparisonItem.Status.LastWriteTimeGroups.ContainsKey(pair.Key))
                    {
                        comparisonItem.Status.LastWriteTimeGroups.Add(pair.Key, new HashSet<InventoryPart>());
                    }

                    comparisonItem.Status.LastWriteTimeGroups[pair.Key].AddAll(pair.Value);

                    foreach (var inventoryPart in pair.Value)
                    {
                        nonfoundInventories.Remove(inventoryPart.Inventory);
                        nonfoundInventoryParts.Remove(inventoryPart);
                    }
                }
            }
        }
        else
        {
            HashSet<Inventory> inventoriesOK = new HashSet<Inventory>();
            HashSet<InventoryPart> inventoryPartsOK = new HashSet<InventoryPart>();
            foreach (var contentIdentity in comparisonItem.ContentIdentities)
            {
                inventoriesOK.AddAll(contentIdentity.GetInventories());
                inventoryPartsOK.AddAll(contentIdentity.GetInventoryParts());
            }

            nonfoundInventories.RemoveAll(inventoriesOK);
            nonfoundInventoryParts.RemoveAll(inventoryPartsOK);
        }


        comparisonItem.Status.MissingInventories.AddAll(nonfoundInventories);
        comparisonItem.Status.MissingInventoryParts.AddAll(nonfoundInventoryParts);
    }

    private ContentIdentityCore BuildContentIdentityCore(InventoryLoader inventoryLoader,
        FileDescription fileDescription)
    {
        var contentIdentityCore = new ContentIdentityCore();

        if (fileDescription.SignatureGuid.IsNotEmpty())
        {
            var memoryStream = inventoryLoader.GetSignature(fileDescription.SignatureGuid);
            contentIdentityCore.SignatureHash =
                $"{CryptographyUtils.ComputeSHA256(memoryStream)}.{memoryStream.Length}/{fileDescription.Size}";
        }

        contentIdentityCore.Size = fileDescription.Size;

        return contentIdentityCore;
    }

    public void Dispose()
    {
        foreach (var inventoryLoader in InventoryLoaders)
        {
            inventoryLoader.Dispose();
        }
    }
}