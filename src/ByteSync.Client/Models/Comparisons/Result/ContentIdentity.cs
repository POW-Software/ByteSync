using System.Text;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;

namespace ByteSync.Models.Comparisons.Result;

public class ContentIdentity
{
    public ContentIdentity(ContentIdentityCore? contentIdentityCore)
    {
        Core = contentIdentityCore;
        
        FileSystemDescriptions = new HashSet<FileSystemDescription>();
        InventoryPartsByCreationTimes = new Dictionary<DateTime, HashSet<InventoryPart>>();
        InventoryPartsByLastWriteTimes = new Dictionary<DateTime, HashSet<InventoryPart>>();
        FileSystemDescriptionsByInventoryParts = new Dictionary<InventoryPart, HashSet<FileSystemDescription>>();
        
        AccessIssueInventoryParts = new HashSet<InventoryPart>();
    }
    
    public ContentIdentityCore? Core { get; }
    
    public HashSet<FileSystemDescription> FileSystemDescriptions { get; }
    
    private Dictionary<DateTime, HashSet<InventoryPart>> InventoryPartsByCreationTimes { get; }
    
    public Dictionary<DateTime, HashSet<InventoryPart>> InventoryPartsByLastWriteTimes { get; }
    
    private Dictionary<InventoryPart, HashSet<FileSystemDescription>> FileSystemDescriptionsByInventoryParts { get; }
    
    // Inventories/parts for which access is known to be an issue (e.g., via ancestor propagation)
    public HashSet<InventoryPart> AccessIssueInventoryParts { get; }
    
    public bool HasAnalysisError
    {
        get { return FileSystemDescriptions.Any(fsd => fsd is FileDescription { HasAnalysisError: true }); }
    }
    
    public bool HasAccessIssue
    {
        get
        {
            return FileSystemDescriptions.Any(fsd => !fsd.IsAccessible)
                   || AccessIssueInventoryParts.Count > 0;
        }
    }
    
    public void AddAccessIssue(InventoryPart inventoryPart)
    {
        AccessIssueInventoryParts.Add(inventoryPart);
    }
    
    public bool HasAccessIssueFor(Inventory inventory)
    {
        // Either explicitly flagged via propagation
        if (AccessIssueInventoryParts.Any(ip => ip.Inventory.Equals(inventory)))
        {
            return true;
        }
        
        // Or present file system descriptions are marked inaccessible for this inventory
        return FileSystemDescriptionsByInventoryParts
            .Any(pair => pair.Key.Inventory.Equals(inventory)
                         && pair.Value.Any(fsd => !fsd.IsAccessible));
    }
    
    public bool HasManyFileSystemDescriptionOnAnInventoryPart
    {
        get { return FileSystemDescriptionsByInventoryParts.Any(p => p.Value.Count > 1); }
    }
    
    protected bool Equals(ContentIdentity other)
    {
        return Equals(Core, other.Core);
    }
    
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        
        if (obj.GetType() != GetType())
        {
            return false;
        }
        
        return Equals((ContentIdentity)obj);
    }
    
    public override int GetHashCode()
    {
        return (Core != null ? Core.GetHashCode() : 0);
    }
    
    public override string ToString()
    {
    #if DEBUG
        var sb = new StringBuilder();
        sb.Append("ContentIdentity ");
        sb.Append(Core?.SignatureHash);
        sb.Append(' ');
        sb.Append(Core?.Size);
        
        foreach (var inventoryPartsByDate in InventoryPartsByLastWriteTimes)
        {
            sb.Append(" - '");
            sb.Append(inventoryPartsByDate.Key.ToString("G"));
            sb.Append("' ");
            sb.Append(string.Join(", ", inventoryPartsByDate.Value.Select(ip => ip.RootName)));
        }
        
        return sb.ToString();
    #endif
        
#pragma warning disable 162
        return base.ToString();
#pragma warning restore 162
    }
    
    public bool IsPresentIn(InventoryPart inventoryPart)
    {
        return FileSystemDescriptionsByInventoryParts.Any(pair => pair.Key.Equals(inventoryPart));
    }
    
    public bool IsPresentIn(Inventory inventory)
    {
        return FileSystemDescriptionsByInventoryParts.Any(pair => pair.Key.Inventory.Equals(inventory));
    }
    
    public HashSet<Inventory> GetInventories()
    {
        var inventories = new HashSet<Inventory>();
        
        foreach (var pair in FileSystemDescriptionsByInventoryParts)
        {
            inventories.Add(pair.Key.Inventory);
        }
        
        return inventories;
    }
    
    public HashSet<InventoryPart> GetInventoryParts()
    {
        var result = new HashSet<InventoryPart>();
        
        foreach (var pair in FileSystemDescriptionsByInventoryParts)
        {
            result.Add(pair.Key);
        }
        
        return result;
    }
    
    public DateTime? GetLastWriteTimeUtc(InventoryPart inventoryPart)
    {
        foreach (var pair in InventoryPartsByLastWriteTimes)
        {
            if (pair.Value.Contains(inventoryPart))
            {
                return pair.Key;
            }
        }
        
        return null;
    }
    
    public DateTime? GetCreationTimeUtc(InventoryPart inventoryPart)
    {
        foreach (var pair in InventoryPartsByCreationTimes)
        {
            if (pair.Value.Contains(inventoryPart))
            {
                return pair.Key;
            }
        }
        
        return null;
    }
    
    public void Add(FileSystemDescription fileSystemDescription)
    {
        FileSystemDescriptions.Add(fileSystemDescription);
        
        if (!FileSystemDescriptionsByInventoryParts.ContainsKey(fileSystemDescription.InventoryPart))
        {
            FileSystemDescriptionsByInventoryParts.Add(fileSystemDescription.InventoryPart, new HashSet<FileSystemDescription>());
        }
        
        FileSystemDescriptionsByInventoryParts[fileSystemDescription.InventoryPart].Add(fileSystemDescription);
        
        if (fileSystemDescription is FileDescription fileDescription)
        {
            AddInventoryPartByCreationTime(fileSystemDescription.InventoryPart, fileDescription.CreationTimeUtc);
            AddInventoryPartByLastWriteTime(fileSystemDescription.InventoryPart, fileDescription.LastWriteTimeUtc);
        }
    }
    
    private void AddInventoryPartByCreationTime(InventoryPart inventoryPart, DateTime creationTimeUtc)
    {
        if (!InventoryPartsByCreationTimes.ContainsKey(creationTimeUtc))
        {
            InventoryPartsByCreationTimes.Add(creationTimeUtc, new HashSet<InventoryPart>());
        }
        
        InventoryPartsByCreationTimes[creationTimeUtc].Add(inventoryPart);
    }
    
    private void AddInventoryPartByLastWriteTime(InventoryPart inventoryPart, DateTime lastWriteTimeUtc)
    {
        if (!InventoryPartsByLastWriteTimes.ContainsKey(lastWriteTimeUtc))
        {
            InventoryPartsByLastWriteTimes.Add(lastWriteTimeUtc, new HashSet<InventoryPart>());
        }
        
        InventoryPartsByLastWriteTimes[lastWriteTimeUtc].Add(inventoryPart);
    }
    
    public HashSet<FileSystemDescription> GetFileSystemDescriptions(InventoryPart inventoryPart)
    {
        if (FileSystemDescriptionsByInventoryParts.TryGetValue(inventoryPart, out var result))
        {
            return result;
        }
        else
        {
            return new HashSet<FileSystemDescription>();
        }
    }
}