using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Inventories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.Business.DataSources;

public class DataSource : ReactiveObject
{
    public DataSource()
    {

    }
    
    public string Id { get; set; } = null!;
    
    public FileSystemTypes Type { get; set; }

    public string Path { get; set; }

    [Reactive]
    public string Code { get; set; }

    public string ClientInstanceId { get; set; }
    
    public string DataNodeId { get; set; }

    public string Key
    {
        get
        {
            return DataNodeId + "_" + Type + "_" + Path;
        }
    }

    public DateTime InitialTimestamp { get; set; }

    protected bool Equals(DataSource other)
    {
        return Type == other.Type && Path == other.Path && DataNodeId == other.DataNodeId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DataSource)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Path, DataNodeId);
    }

    public bool BelongsTo(SessionMember sessionMember)
    {
        return ClientInstanceId.IsNotEmpty() && ClientInstanceId.Equals(sessionMember.ClientInstanceId);
    }
    
    public bool BelongsTo(DataNode dataNode)
    {
        return DataNodeId.IsNotEmpty() && DataNodeId.Equals(dataNode.Id);
    }
}