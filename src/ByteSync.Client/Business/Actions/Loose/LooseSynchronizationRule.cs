using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Interfaces.Business;
using ByteSync.Interfaces.Business.Actions;

namespace ByteSync.Business.Actions.Loose;

public class LooseSynchronizationRule : ISynchronizationRule
{
    public LooseSynchronizationRule()
    {
        Conditions = new List<LooseAtomicCondition>();
        Actions = new List<LooseAtomicAction>();
    }
    
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
    
    public List<LooseAtomicCondition> Conditions { get; set; }
    
    public List<LooseAtomicAction> Actions { get; set; }
}