using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Interfaces.Business;
using ByteSync.Interfaces.Business.Actions;

namespace ByteSync.Business.Actions.Local;

public class SynchronizationRule : ISynchronizationRule
{
    public SynchronizationRule(FileSystemTypes fileSystemType, ConditionModes conditionMode)
    {
        SynchronizationRuleId = Guid.NewGuid().ToString();

        FileSystemType = fileSystemType;
        ConditionMode = conditionMode;
            
        Conditions = new List<AtomicCondition>();
        Actions = new List<AtomicAction>();
    }

    public string SynchronizationRuleId { get; set; }
        
    public FileSystemTypes FileSystemType { get; set; }
        
    public ConditionModes ConditionMode { get; set; }
        
    public List<IAtomicCondition> GetConditions()
    {
        return new List<IAtomicCondition>(Conditions);
    }

    public List<IAtomicAction> GetActions()
    {
        return new List<IAtomicAction>(Actions);
    }

    internal List<AtomicCondition> Conditions { get; set; }
        
    internal List<AtomicAction> Actions { get; set; }
        
    internal void AddAction(AtomicAction atomicAction)
    {
        atomicAction.SynchronizationRule = this;
            
        Actions.Add(atomicAction);
    }

    protected bool Equals(SynchronizationRule other)
    {
        return SynchronizationRuleId == other.SynchronizationRuleId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SynchronizationRule) obj);
    }

    public override int GetHashCode()
    {
        return (SynchronizationRuleId != null ? SynchronizationRuleId.GetHashCode() : 0);
    }
}