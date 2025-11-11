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
    private readonly IInitialStatusBuilder _initialStatusBuilder;
    
    public InventoryComparer(SessionSettings sessionSettings, IInitialStatusBuilder initialStatusBuilder,
        InventoryIndexer? inventoryIndexer = null)
    {
        SessionSettings = sessionSettings;
        Indexer = inventoryIndexer;
        
        _initialStatusBuilder = initialStatusBuilder;
        
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
    
    public IInventoryIndexer? Indexer { get; set; }
    
    public ComparisonResult Compare()
    {
        ComparisonResult.Clear();
        
        foreach (var inventoryLoader in InventoryLoaders.OrderBy(il => il.Inventory.Code))
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
                        HandleDirectoryDescription(directoryDescription);
                    }
                }
            }
        }
        
        foreach (var comparisonItem in ComparisonResult.ComparisonItems)
        {
            _initialStatusBuilder.BuildStatus(comparisonItem, InventoryLoaders.Select(il => il.Inventory));
        }
        
        // Propagate access issues from inaccessible ancestor directories (Tree mode only)
        if (SessionSettings.MatchingMode == MatchingModes.Tree)
        {
            PropagateAccessIssuesFromAncestors();
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
    
    private void HandleDirectoryDescription(DirectoryDescription directoryDescription)
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
    
    private void PropagateAccessIssuesFromAncestors()
    {
        // Build a table of inaccessible directories per inventory part (relative paths)
        var inaccessibleByPart = new Dictionary<InventoryPart, HashSet<string>>();
        foreach (var loader in InventoryLoaders)
        {
            foreach (var part in loader.Inventory.InventoryParts)
            {
                var set = new HashSet<string>(StringComparer.Ordinal);
                foreach (var dir in part.DirectoryDescriptions)
                {
                    if (!dir.IsAccessible)
                    {
                        set.Add(dir.RelativePath);
                    }
                }
                
                inaccessibleByPart[part] = set;
            }
        }
        
        // For each item, check if it's under an inaccessible ancestor
        foreach (var item in ComparisonResult.ComparisonItems)
        {
            var relative = item.PathIdentity.LinkingKeyValue; // e.g., "/a/b/c.txt"
            if (string.IsNullOrWhiteSpace(relative) || relative == "/")
            {
                continue;
            }
            
            // Track which inventory parts have the item under an inaccessible ancestor
            var partsWithInaccessibleAncestor = new HashSet<InventoryPart>();
            
            foreach (var loader in InventoryLoaders)
            {
                foreach (var part in loader.Inventory.InventoryParts)
                {
                    if (IsUnderInaccessibleAncestor(relative, inaccessibleByPart[part]))
                    {
                        partsWithInaccessibleAncestor.Add(part);
                    }
                }
            }
            
            if (partsWithInaccessibleAncestor.Count == 0)
            {
                continue;
            }
            
            // Check which inventory parts are present in existing ContentIdentities
            var partsWithContent = item.ContentIdentities
                .SelectMany(ci => ci.GetInventoryParts())
                .ToHashSet();
            
            // For files, create a virtual ContentIdentity for parts missing due to inaccessible ancestor
            if (item.FileSystemType == FileSystemTypes.File)
            {
                foreach (var part in partsWithInaccessibleAncestor)
                {
                    // Check if this specific part already has content for this item
                    var hasExistingContentForPart = partsWithContent.Contains(part);
                    
                    if (!hasExistingContentForPart)
                    {
                        // Create a virtual ContentIdentity for this inaccessible file
                        var virtualContentIdentity = new ContentIdentity(null);
                        
                        // Create a minimal FileDescription marked as inaccessible
                        var virtualFileDescription = new FileDescription(part, relative)
                        {
                            IsAccessible = false
                        };
                        
                        virtualContentIdentity.Add(virtualFileDescription);
                        virtualContentIdentity.AddAccessIssue(part);
                        
                        item.AddContentIdentity(virtualContentIdentity);
                    }
                    
                    // If the part already exists (which shouldn't happen for inaccessible ancestors), skip it
                }
            }
            else
            {
                // For directories, just mark existing ContentIdentities
                foreach (var part in partsWithInaccessibleAncestor)
                {
                    foreach (var ci in item.ContentIdentities)
                    {
                        ci.AddAccessIssue(part);
                    }
                }
            }
        }
    }
    
    private static bool IsUnderInaccessibleAncestor(string relativePath, HashSet<string> inaccessibleDirs)
    {
        // Walk parents: /a/b/c.txt -> /a/b -> /a
        var path = relativePath;
        while (true)
        {
            var idx = path.LastIndexOf('/');
            if (idx <= 0)
            {
                return false;
            }
            
            path = path.Substring(0, idx); // drop last segment
            if (inaccessibleDirs.Contains(path))
            {
                return true;
            }
        }
    }
    
    private PathIdentity BuildPathIdentity(FileSystemDescription fileSystemDescription)
    {
        string linkingData;
        if (SessionSettings.MatchingMode == MatchingModes.Tree)
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
        else if (SessionSettings.MatchingMode == MatchingModes.Flat)
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
            throw new ArgumentOutOfRangeException(nameof(SessionSettings.MatchingMode));
        }
        
        string linkingKeyValue;
        if (SessionSettings.MatchingMode == MatchingModes.Tree)
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
    
    private ContentIdentityCore BuildContentIdentityCore(InventoryLoader inventoryLoader,
        FileDescription fileDescription)
    {
        var contentIdentityCore = new ContentIdentityCore();
        
        if (fileDescription.SignatureGuid.IsNotEmpty())
        {
            var memoryStream = inventoryLoader.GetSignature(fileDescription.SignatureGuid!);
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
        
        _initialStatusBuilder.Dispose();
    }
}