using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Interfaces.Business;

namespace ByteSync.Interfaces.Business.Actions;

public interface ISynchronizationRule
{
    public FileSystemTypes FileSystemType { get; }
    
    public ConditionModes ConditionMode { get; }

    public List<IAtomicCondition> GetConditions();
    
    public List<IAtomicAction> GetActions();
}