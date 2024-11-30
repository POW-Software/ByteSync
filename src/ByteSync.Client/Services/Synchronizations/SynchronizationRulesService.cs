using ByteSync.Business.Actions.Local;
using ByteSync.Business.Actions.Loose;
using ByteSync.Interfaces.Controls.Profiles;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationRulesService : ISynchronizationRulesService
{
    private readonly ISynchronizationRuleRepository _synchronizationRuleRepository;
    private readonly IDataPartIndexer _dataPartIndexer;
    private readonly ISynchronizationRuleMatcher _synchronizationRuleMatcher;
    private readonly IComparisonItemsService _comparisonItemsService;
    private readonly ISynchronizationRulesConverter _synchronizationRulesConverter;

    public SynchronizationRulesService(ISynchronizationRuleRepository synchronizationRuleRepository, IDataPartIndexer dataPartIndexer, 
        ISynchronizationRuleMatcher synchronizationRuleMatcher,
        IComparisonItemsService comparisonItemsService, ISynchronizationRulesConverter synchronizationRulesConverter)
    {
        _synchronizationRuleRepository = synchronizationRuleRepository;
        _dataPartIndexer = dataPartIndexer;
        _synchronizationRuleMatcher = synchronizationRuleMatcher;
        _comparisonItemsService = comparisonItemsService;
        _synchronizationRulesConverter = synchronizationRulesConverter;
        
        // SynchronizationRulesCache = new SourceCache<SynchronizationRule, string>(synchronizationRule => synchronizationRule.SynchronizationRuleId);
        // SynchronizationRules = SynchronizationRulesCache.Connect().Publish().AsObservableCache();
    }

    // private SourceCache<SynchronizationRule, string> SynchronizationRulesCache { get; set; }
    //
    // public IObservableCache<SynchronizationRule, string> SynchronizationRules { get; set; }
    
    public void AddSynchronizationRule(SynchronizationRule synchronizationRule)
    {
        var allSynchronizationRules = _synchronizationRuleRepository.Elements.ToList();
        allSynchronizationRules.Add(synchronizationRule);
        
        var allComparisonItems = _comparisonItemsService.ComparisonItems.Items.ToList();
        
        _dataPartIndexer.Remap(allSynchronizationRules);
        _synchronizationRuleMatcher.MakeMatches(allComparisonItems, allSynchronizationRules);
        
        // var allSynchronizationRules = SynchronizationRules!.Select(vm => vm.SynchronizationRule).ToList();
        // await _uiHelper.ExecuteOnUi(() =>
        // {
        //     SynchronizationRuleMatcher synchronizationRuleMatcher = new SynchronizationRuleMatcher(this);
        //     synchronizationRuleMatcher.MakeMatches(ComparisonItems, allSynchronizationRules);
        // });
        
        
        
        // _comparisonItemsService.ApplySynchronizationRules();
        
        _synchronizationRuleRepository.AddOrUpdate(synchronizationRule);
    }

    // public void ClearSynchronizationRules()
    // {
    //     SynchronizationRulesCache.Clear();
    // }

    public List<LooseSynchronizationRule> GetLooseSynchronizationRules()
    {
        return _synchronizationRulesConverter.ConvertLooseSynchronizationRules(_synchronizationRuleRepository.Elements.ToList());
    }
}