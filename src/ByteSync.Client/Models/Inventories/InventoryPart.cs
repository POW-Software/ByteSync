using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Models.FileSystems;

namespace ByteSync.Models.Inventories;

public class InventoryPart
{
    public InventoryPart()
    {
        FileDescriptions = new List<FileDescription>();
        DirectoryDescriptions = new List<DirectoryDescription>();
    }
    
    public InventoryPart(Inventory inventory, string rootPath, FileSystemTypes inventoryPartType) : this()
    {
        Inventory = inventory;
        RootPath = rootPath;
        InventoryPartType = inventoryPartType;
    }
    
    public Inventory Inventory { get; set; }
    
    public string RootPath { get; set; }
    
    public FileSystemTypes InventoryPartType { get; set; }
    
    public string Code { get; set; }
    
    public List<FileDescription> FileDescriptions { get; set; }
    
    public List<DirectoryDescription> DirectoryDescriptions { get; set; }
    
    public bool IsIncompleteDueToAccess { get; set; }
    
    public Dictionary<SkipReason, int> SkippedCountsByReason { get; set; } = new();
    
    public int SkippedCount => SkippedCountsByReason.Values.Sum();
    
    public string RootName
    {
        get
        {
            string directorySeparatorChar;
            switch (Inventory.Endpoint.OSPlatform)
            {
                case OSPlatforms.Windows:
                    directorySeparatorChar = "\\";
                    
                    break;
                case OSPlatforms.Linux:
                case OSPlatforms.MacOs:
                    directorySeparatorChar = "/";
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(directorySeparatorChar));
            }
            
            return RootPath.Substring(RootPath.LastIndexOf(directorySeparatorChar, StringComparison.Ordinal));
        }
    }
    
    protected bool Equals(InventoryPart other)
    {
        return Equals(Inventory, other.Inventory) && RootPath == other.RootPath && InventoryPartType == other.InventoryPartType;
    }
    
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        
        return Equals((InventoryPart)obj);
    }
    
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Inventory.GetHashCode();
            hashCode = (hashCode * 397) ^ RootPath.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)InventoryPartType;
            
            return hashCode;
        }
    }
    
    public override string ToString()
    {
    #if DEBUG
        return $"InventoryPart {RootName} {RootPath}";
    #endif
        
#pragma warning disable 162
        return base.ToString();
#pragma warning restore 162
    }
    
    public void AddFileSystemDescription(FileSystemDescription fileSystemDescription)
    {
        if (fileSystemDescription.FileSystemType == FileSystemTypes.File)
        {
            FileDescriptions.Add((FileDescription)fileSystemDescription);
        }
        else
        {
            DirectoryDescriptions.Add((DirectoryDescription)fileSystemDescription);
        }
    }
    
    public int GetSkippedCountByReason(SkipReason reason)
    {
        return SkippedCountsByReason.TryGetValue(reason, out var count) ? count : 0;
    }
    
    public void RecordSkippedEntry(SkipReason reason)
    {
        SkippedCountsByReason[reason] = GetSkippedCountByReason(reason) + 1;
    }
}