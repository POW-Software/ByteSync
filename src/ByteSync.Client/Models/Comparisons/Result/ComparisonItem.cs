using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Inventories;

namespace ByteSync.Models.Comparisons.Result;

public class ComparisonItem
{
    public ComparisonItem(PathIdentity pathIdentity)
    {
        PathIdentity = pathIdentity;
        ContentIdentities = new HashSet<ContentIdentity>();
        ContentRepartition = new ContentRepartition(PathIdentity);
        ItemSynchronizationStatus = new ItemSynchronizationStatus(PathIdentity);
    }

    public PathIdentity PathIdentity { get; }

    public HashSet<ContentIdentity> ContentIdentities { get; }
        
    public ComparisonResult ComparisonResult { get; set; }

    public ContentRepartition ContentRepartition { get; set; }
    
    public ItemSynchronizationStatus ItemSynchronizationStatus { get; set; }

    public FileSystemTypes FileSystemType
    {
        get
        {
            return PathIdentity.FileSystemType;
        }
    }

    public void AddContentIdentity(ContentIdentity contentIdentity)
    {
        ContentIdentities.Add(contentIdentity);
    }

    public ContentIdentity? GetContentIdentity(ContentIdentityCore contentIdentityCore)
    {
        return ContentIdentities.SingleOrDefault(ci => Equals(ci.Core, contentIdentityCore));
    }

    protected bool Equals(ComparisonItem other)
    {
        return Equals(PathIdentity, other.PathIdentity);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ComparisonItem) obj);
    }

    public override int GetHashCode()
    {
        return PathIdentity.GetHashCode();
    }

    public List<ContentIdentity> GetContentIdentities(InventoryPart inventoryPart)
    {
        return ContentIdentities.Where(ci => ci.IsPresentIn(inventoryPart)).ToList();
    }
}