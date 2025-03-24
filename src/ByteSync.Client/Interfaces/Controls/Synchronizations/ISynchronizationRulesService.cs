using ByteSync.Business.Actions.Local;
using ByteSync.Business.Actions.Loose;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationRulesService
{
    void AddOrUpdateSynchronizationRule(SynchronizationRule synchronizationRule);
    
    void Remove(SynchronizationRule synchronizationRule);
    
    void Clear();
    
    List<LooseSynchronizationRule> GetLooseSynchronizationRules();
}