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
        var allSynchronizationRules = _synchronizationRuleRepository.Elements.ToHashSet();
        allSynchronizationRules.Remove(synchronizationRule);
        allSynchronizationRules.Add(synchronizationRule);
        
        var allComparisonItems = _comparisonItemRepository.Elements.ToList();
        
        _dataPartIndexer.Remap(allSynchronizationRules);
        _synchronizationRuleMatcher.MakeMatches(allComparisonItems, allSynchronizationRules);
        
        _synchronizationRuleRepository.AddOrUpdate(synchronizationRule);
    }

    public void Remove(SynchronizationRule synchronizationRule)
    {
        _synchronizationRuleRepository.Remove(synchronizationRule);
        
        var allSynchronizationRules = _synchronizationRuleRepository.Elements.ToList();
        var allComparisonItems = _comparisonItemRepository.Elements.ToList();
        
        _dataPartIndexer.Remap(allSynchronizationRules);
        _synchronizationRuleMatcher.MakeMatches(allComparisonItems, allSynchronizationRules);
    }

    public void Clear()
    {
        _synchronizationRuleRepository.Clear();
        
        var allSynchronizationRules = _synchronizationRuleRepository.Elements.ToList();
        var allComparisonItems = _comparisonItemRepository.Elements.ToList();
        
        _dataPartIndexer.Remap(allSynchronizationRules);
        _synchronizationRuleMatcher.MakeMatches(allComparisonItems, new List<SynchronizationRule>());
    }
    
    public List<LooseSynchronizationRule> GetLooseSynchronizationRules()
    {
        return _synchronizationRulesConverter.ConvertLooseSynchronizationRules(_synchronizationRuleRepository.Elements.ToList());
    }
}