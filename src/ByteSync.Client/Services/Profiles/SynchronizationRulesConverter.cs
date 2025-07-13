using ByteSync.Business.Actions.Local;
using ByteSync.Business.Actions.Loose;
using ByteSync.Interfaces.Controls.Profiles;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Services.Profiles;

public class SynchronizationRulesConverter : ISynchronizationRulesConverter
{
    private readonly ISynchronizationRuleSummaryViewModelFactory _synchronizationRuleSummaryViewModelFactory;
    private readonly IDataPartIndexer _dataPartIndexer;

    public SynchronizationRulesConverter(ISynchronizationRuleSummaryViewModelFactory synchronizationRuleSummaryViewModelFactory, IDataPartIndexer dataPartIndexer)
    {
        _synchronizationRuleSummaryViewModelFactory = synchronizationRuleSummaryViewModelFactory;
        _dataPartIndexer = dataPartIndexer;
    }
    
    public List<LooseSynchronizationRule> ConvertLooseSynchronizationRules(
        ICollection<SynchronizationRule> synchronizationRules)
    {
        var result = new List<LooseSynchronizationRule>();
        
        foreach (var synchronizationRule in synchronizationRules)
        {
            var looseSynchronizationRule = new LooseSynchronizationRule();

            looseSynchronizationRule.FileSystemType = synchronizationRule.FileSystemType;
            looseSynchronizationRule.ConditionMode = synchronizationRule.ConditionMode;
            foreach (var condition in synchronizationRule.Conditions)
            {
                var looseAtomicCondition = new LooseAtomicCondition();
                looseAtomicCondition.SourceName = condition.Source.Name;
                looseAtomicCondition.DestinationName = condition.Destination?.Name;
                looseAtomicCondition.Size = condition.Size;
                looseAtomicCondition.ComparisonProperty = condition.ComparisonProperty;
                looseAtomicCondition.ConditionOperator = condition.ConditionOperator;
                looseAtomicCondition.DateTime = condition.DateTime;
                looseAtomicCondition.SizeUnit = condition.SizeUnit;
                looseAtomicCondition.NamePattern = condition.NamePattern;

                looseSynchronizationRule.Conditions.Add(looseAtomicCondition);
            }
            
            foreach (var action in synchronizationRule.Actions)
            {
                var looseAtomicAction = new LooseAtomicAction();

                looseAtomicAction.SourceName = action.Source?.Name;
                looseAtomicAction.DestinationName = action.Destination?.Name;
                looseAtomicAction.Operator = action.Operator;

                looseSynchronizationRule.Actions.Add(looseAtomicAction);
            }

            result.Add(looseSynchronizationRule);
        }

        return result;
    }

    public bool CheckAllDataPartsAreMappable(List<LooseSynchronizationRule> looseSynchronizationRules)
    {
        foreach (var profileDetailsSynchronizationRule in looseSynchronizationRules)
        {
            foreach (var condition in profileDetailsSynchronizationRule.Conditions)
            {
                if (!IsDataPartMappableOrUnset(condition.SourceName))
                {
                    return false;
                }
                
                if (!IsDataPartMappableOrUnset(condition.DestinationName))
                {
                    return false;
                }

            }

            foreach (var action in profileDetailsSynchronizationRule.Actions)
            {
                if (!IsDataPartMappableOrUnset(action.SourceName))
                {
                    return false;
                }
                
                if (!IsDataPartMappableOrUnset(action.DestinationName))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsDataPartMappableOrUnset(string? dataPartName)
    {
        if (dataPartName == null)
        {
            return true;
        }
        else
        {
            return _dataPartIndexer.GetDataPart(dataPartName) != null;
        }
    }

    public List<SynchronizationRuleSummaryViewModel> ConvertToSynchronizationRuleViewModels(
        List<LooseSynchronizationRule> looseSynchronizationRules)
    {
        var result = new List<SynchronizationRuleSummaryViewModel>();

        foreach (var profileDetailsSynchronizationRule in looseSynchronizationRules)
        {
            var synchronizationRule = new SynchronizationRule(profileDetailsSynchronizationRule.FileSystemType, 
                profileDetailsSynchronizationRule.ConditionMode);
            
            foreach (var condition in profileDetailsSynchronizationRule.Conditions)
            {
                var atomicCondition = new AtomicCondition();
                atomicCondition.Source = _dataPartIndexer.GetDataPart(condition.SourceName)!;
                atomicCondition.Destination = _dataPartIndexer.GetDataPart(condition.DestinationName);
                atomicCondition.Size = condition.Size;
                atomicCondition.ComparisonProperty = condition.ComparisonProperty;
                atomicCondition.ConditionOperator = condition.ConditionOperator;
                atomicCondition.DateTime = condition.DateTime;
                atomicCondition.SizeUnit = condition.SizeUnit;
                atomicCondition.NamePattern = condition.NamePattern;

                synchronizationRule.Conditions.Add(atomicCondition);
            }
            
            foreach (var action in profileDetailsSynchronizationRule.Actions)
            {
                var atomicAction = new AtomicAction();
                atomicAction.AtomicActionId = $"AAID_{Guid.NewGuid()}";

                atomicAction.Source = _dataPartIndexer.GetDataPart(action.SourceName);
                atomicAction.Destination = _dataPartIndexer.GetDataPart(action.DestinationName);
                atomicAction.Operator = action.Operator;

                synchronizationRule.AddAction(atomicAction);
            }

            var synchronizationRuleSummaryViewModel = _synchronizationRuleSummaryViewModelFactory.Create(synchronizationRule);

            result.Add(synchronizationRuleSummaryViewModel);
        }
        
        return result;
    }
}