using ByteSync.Business.Actions.Local;
using ByteSync.Business.Actions.Loose;
using ByteSync.Interfaces.Controls.Profiles;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationRulesService : ISynchronizationRulesService
{
    private readonly ISynchronizationRuleRepository _synchronizationRuleRepository;
    private readonly IDataPartIndexer _dataPartIndexer;
    private readonly ISynchronizationRuleMatcher _synchronizationRuleMatcher;
    private readonly IComparisonItemRepository _comparisonItemRepository;
    private readonly ISynchronizationRulesConverter _synchronizationRulesConverter;

    public SynchronizationRulesService(ISynchronizationRuleRepository synchronizationRuleRepository, IDataPartIndexer dataPartIndexer, 
        ISynchronizationRuleMatcher synchronizationRuleMatcher,
        IComparisonItemRepository comparisonItemRepository, ISynchronizationRulesConverter synchronizationRulesConverter)
    {
        _synchronizationRuleRepository = synchronizationRuleRepository;
        _dataPartIndexer = dataPartIndexer;
        _synchronizationRuleMatcher = synchronizationRuleMatcher;
        _comparisonItemRepository = comparisonItemRepository;
        _synchronizationRulesConverter = synchronizationRulesConverter;
    }
    
    public void AddOrUpdateSynchronizationRule(SynchronizationRule synchronizationRule)
    {
        var updatedSynchronizationRules = _synchronizationRuleRepository.Elements.ToHashSet();
        updatedSynchronizationRules.Remove(synchronizationRule);
        updatedSynchronizationRules.Add(synchronizationRule);
        
        RefreshRulesAndMatches(updatedSynchronizationRules);

        _synchronizationRuleRepository.AddOrUpdate(synchronizationRule);
    }
    public void Remove(SynchronizationRule synchronizationRule)
    {
        _synchronizationRuleRepository.Remove(synchronizationRule);
        
        RefreshRulesAndMatches();
    }

    public void Clear()
    {
        _synchronizationRuleRepository.Clear();
        
        RefreshRulesAndMatches();
    }
    
    public List<LooseSynchronizationRule> GetLooseSynchronizationRules()
    {
        return _synchronizationRulesConverter.ConvertLooseSynchronizationRules(_synchronizationRuleRepository.Elements.ToList());
    }
    
    private void RefreshRulesAndMatches(HashSet<SynchronizationRule>? allSynchronizationRules = null)
    {
        allSynchronizationRules ??= _synchronizationRuleRepository.Elements.ToHashSet();
        
        var allComparisonItems = _comparisonItemRepository.Elements.ToList();
        
        _dataPartIndexer.Remap(allSynchronizationRules);
        _synchronizationRuleMatcher.MakeMatches(allComparisonItems, allSynchronizationRules);
    }
}