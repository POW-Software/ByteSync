using ByteSync.Business.Actions.Local;
using ByteSync.Business.Actions.Loose;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationRulesService
{
    // public IObservableCache<SynchronizationRule, string> SynchronizationRules { get; }
    
    void AddSynchronizationRule(SynchronizationRule synchronizationRule);
    
    // void ClearSynchronizationRules();
    
    List<LooseSynchronizationRule> GetLooseSynchronizationRules();
}