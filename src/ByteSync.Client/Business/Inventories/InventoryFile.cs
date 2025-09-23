using ByteSync.Business.Communications;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Business.Inventories;

public class InventoryFile : IEquatable<InventoryFile>
{
    public InventoryFile(SharedFileDefinition sharedFileDefinition, string fullName)
    {
        SharedFileDefinition = sharedFileDefinition;
        FullName = fullName;
    }
    
    public InventoryFile(LocalSharedFile localSharedFile)
    {
        SharedFileDefinition = localSharedFile.SharedFileDefinition;
        FullName = localSharedFile.LocalPath;
    }
    
    public SharedFileDefinition SharedFileDefinition { get; }
    
    public string FullName { get; }
    
    public bool IsBaseInventory
    {
        get { return SharedFileDefinition.SharedFileType == SharedFileTypes.BaseInventory; }
    }
    
    public bool IsFullInventory
    {
        get { return SharedFileDefinition.SharedFileType == SharedFileTypes.FullInventory; }
    }
    
    public LocalInventoryModes LocalInventoryMode
    {
        get
        {
            if (IsBaseInventory)
            {
                return LocalInventoryModes.Base;
            }
            else
            {
                return LocalInventoryModes.Full;
            }
        }
    }
    
    public bool Equals(InventoryFile? other)
    {
        return FullName == other?.FullName;
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
        
        if (obj.GetType() != this.GetType())
        {
            return false;
        }
        
        return Equals((InventoryFile)obj);
    }
    
    public override int GetHashCode()
    {
        return FullName.GetHashCode();
    }
}