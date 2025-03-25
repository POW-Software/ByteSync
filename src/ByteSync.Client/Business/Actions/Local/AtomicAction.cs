using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Interfaces.Business;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Actions.Local;

public class AtomicAction : AbstractAction, IAtomicAction
{
    public AtomicAction()
    {
            
    }

    public AtomicAction(string atomicActionId, ComparisonItem comparisonItem)
    {
        AtomicActionId = atomicActionId;
        ComparisonItem = comparisonItem;
    }

    public string AtomicActionId { get; set; } = null!;
        
    public ComparisonItem? ComparisonItem { get; set; }

    public DataPart? Source { get; set; }
        
    public DataPart? Destination { get; set; }

    public string? SourceName
    {
        get
        {
            return Source?.Name;
        }
    }

    public string? DestinationName
    {
        get
        {
            return Destination?.Name;
        }
    }

    public SynchronizationRule? SynchronizationRule { get; set; }
        
    public bool IsFromSynchronizationRule
    {
        get
        {
            return SynchronizationRule != null;
        }
    }

    public bool IsTargeted
    {
        get
        {
            return !IsFromSynchronizationRule;
        }
    }

    public PathIdentity? PathIdentity => ComparisonItem?.PathIdentity;

    protected bool Equals(AtomicAction other)
    {
        return AtomicActionId == other.AtomicActionId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AtomicAction) obj);
    }

    public override int GetHashCode()
    {
        return (AtomicActionId != null ? AtomicActionId.GetHashCode() : 0);
    }

    public bool IsSimilarTo(AtomicAction atomicAction)
    {
        return Operator == atomicAction.Operator
               && Equals(Source, atomicAction.Source)
               && Equals(Destination, atomicAction.Destination);
    }

    public AtomicAction CloneNew()
    {
        AtomicAction atomicAction = (AtomicAction) this.MemberwiseClone();

        atomicAction.AtomicActionId = $"AAID_{Guid.NewGuid()}";

        return atomicAction;
    }
}