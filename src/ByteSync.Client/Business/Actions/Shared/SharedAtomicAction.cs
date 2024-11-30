using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;

namespace ByteSync.Business.Actions.Shared;

public class SharedAtomicAction : AbstractAction
{
    public SharedAtomicAction()
    {

    }

    public SharedAtomicAction(string atomicActionId)
    {
        AtomicActionId = atomicActionId;
    }

    public string AtomicActionId { get; set; } = null!;
    
    public string? ActionsGroupId { get; set; }

    public SharedDataPart? Source { get; set; }

    public SharedDataPart? Target { get; set; }

    public SynchronizationTypes? SynchronizationType { get; set; }

    public PathIdentity PathIdentity { get; set; } = null!;

    public long? Size { get; set; }

    public DateTime? CreationTimeUtc { get; set; }
    
    public DateTime? LastWriteTimeUtc { get; set; }
    
    public bool IsFromSynchronizationRule { get; set; }

    protected bool Equals(SharedAtomicAction other)
    {
        return AtomicActionId == other.AtomicActionId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SharedAtomicAction)obj);
    }

    public override int GetHashCode()
    {
        return AtomicActionId.GetHashCode();
    }
}