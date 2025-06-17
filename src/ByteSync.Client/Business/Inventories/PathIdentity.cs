using ByteSync.Common.Business.Inventories;

namespace ByteSync.Business.Inventories;

public class PathIdentity
{
    public PathIdentity()
    {
            
    }

    public PathIdentity(FileSystemTypes fileSystemType, string linkingKeyValue, string name, string linkingData)
    {
        FileSystemType = fileSystemType;
        LinkingKeyValue = linkingKeyValue;
        FileName = name;
        LinkingData = linkingData;
        // InventorySourceType = inventoryPartType;
    }
        
    public FileSystemTypes FileSystemType { get; set; }

    public string LinkingKeyValue { get; set; }
        
    public string FileName { get; set; }
        
    public string LinkingData { get; set; }
    

    protected bool Equals(PathIdentity other)
    {
        return FileSystemType == other.FileSystemType && string.Equals(LinkingData, other.LinkingData);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PathIdentity)obj);
    }

    public override int GetHashCode()
    {
        return LinkingData.GetHashCode();
    }
}