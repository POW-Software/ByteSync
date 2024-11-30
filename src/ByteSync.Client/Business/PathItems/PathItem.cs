using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Helpers;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.PathItems;

public class PathItem
{
    public PathItem()
    {

    }
    
    public FileSystemTypes Type { get; set; }

    public string Path { get; set; }

    [Reactive]
    public string Code { get; set; }

    public string ClientInstanceId { get; set; }

    public string Key
    {
        get
        {
            return ClientInstanceId + "_" + Type + "_" + Path;
        }
    }

    protected bool Equals(PathItem other)
    {
        return Type == other.Type && Path == other.Path && ClientInstanceId == other.ClientInstanceId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PathItem)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Path, ClientInstanceId);
    }

    public bool BelongsTo(SessionMemberInfo sessionMemberInfo)
    {
        return ClientInstanceId.IsNotEmpty() && ClientInstanceId.Equals(sessionMemberInfo.ClientInstanceId);
    }
}