using System.Text;
using ByteSync.Assets.Resources;
using ByteSync.Business.Comparisons;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Business.Actions;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Services.Comparisons.DescriptionBuilders;

public class SynchronizationRuleDescriptionBuilder
{
    private readonly ILocalizationService _localizationService;
    private readonly IDescriptionBuilderFactory _descriptionBuilderFactory;

    public SynchronizationRuleDescriptionBuilder(ISynchronizationRule synchronizationRule, ILocalizationService localizationService,
        IDescriptionBuilderFactory descriptionBuilderFactory)
    {
        SynchronizationRule = synchronizationRule;

        _localizationService = localizationService;
        _descriptionBuilderFactory = descriptionBuilderFactory;
    }
    
    public ISynchronizationRule SynchronizationRule { get; }
    
    public string? Mode { get; set; }
    
    public string? Conditions { get; set; }
    
    public string? Actions { get; set; }
    
    public string? Then { get; set; }

    public void BuildDescription(string conditionsActionsSeparator)
    {
        BuildMode();

        BuildConditions(conditionsActionsSeparator);
        
        BuildActions(conditionsActionsSeparator);
        
        Then = _localizationService[nameof(Resources.SynchronizationRuleSummary_Then)];
    }

    private void BuildMode()
    {
        string mode;
        if (SynchronizationRule.GetConditions().Count == 1)
        {
            mode = Resources.SynchronizationRuleSummary_If;
        }
        else
        {
            if (SynchronizationRule.ConditionMode == ConditionModes.All)
            {
                mode = Resources.SynchronizationRuleSummary_IfAll;
            }
            else
            {
                mode = Resources.SynchronizationRuleSummary_IfAny;
            }
        }

        Mode = mode;
    }
    
    private void BuildConditions(string conditionsActionsSeparator)
    {
        var sbConditions = new StringBuilder();
        var atomicConditionDescriptionBuilder = _descriptionBuilderFactory.CreateAtomicConditionDescriptionBuilder();
        
        var i = 0;
        var conditions = SynchronizationRule.GetConditions();
        foreach (var atomicCondition in conditions)
        {
            i += 1;
            atomicConditionDescriptionBuilder.AppendDescription(sbConditions, atomicCondition);

            if (i != conditions.Count)
            {
                sbConditions.Append(conditionsActionsSeparator);
            }
        }
        
        Conditions = sbConditions.ToString();
    }

    private void BuildActions(string conditionsActionsSeparator)
    {
        var sbActions = new StringBuilder();
        var synchronizationActionDescriptionBuilder = _descriptionBuilderFactory.CreateAtomicActionDescriptionBuilder();

        var i = 0;
        var actions = SynchronizationRule.GetActions();
        foreach (var atomicAction in actions)
        {
            i += 1;
            synchronizationActionDescriptionBuilder.AppendDescription(sbActions, atomicAction);

            if (i != actions.Count)
            {
                sbActions.Append(conditionsActionsSeparator);
            }
        }
        
        Actions = sbActions.ToString();
    }
}