using ByteSync.Business.Actions.Local;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Models.Comparisons.Result;
using System.Text.RegularExpressions;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Business.Filtering.Evaluators;

public class ActionComparisonExpressionEvaluator : ExpressionEvaluator<ActionComparisonExpression>
{
    private readonly IAtomicActionRepository _actionRepository;

    public ActionComparisonExpressionEvaluator(IAtomicActionRepository actionRepository)
    {
        _actionRepository = actionRepository;
    }
    
    public override bool Evaluate(ActionComparisonExpression expression, ComparisonItem item)
    {
        // Extract parts of the action path (actions, targeted/rules, action type)
        string[] pathParts = expression.ActionPath.Split('.');
        
        int actionCount = GetActionCount(item, pathParts);
        
        // Compare with the specified operator
        return expression.Operator switch
        {
            ComparisonOperator.Equals => actionCount == expression.Value,
            ComparisonOperator.NotEquals => actionCount != expression.Value,
            ComparisonOperator.GreaterThan => actionCount > expression.Value,
            ComparisonOperator.LessThan => actionCount < expression.Value,
            ComparisonOperator.GreaterThanOrEqual => actionCount >= expression.Value,
            ComparisonOperator.LessThanOrEqual => actionCount <= expression.Value,
            _ => throw new ArgumentException($"Unsupported operator for actions: {expression.Operator}")
        };
    }

    private int GetActionCount(ComparisonItem item, string[] pathParts)
    {
        // The base of the path is always "actions"
        if (pathParts.Length == 0 || !pathParts[0].Equals(Identifiers.OPERATOR_ACTIONS, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }
            
        // Get the full list of actions
        var allActions = _actionRepository.GetAtomicActions(item);

        if (!allActions.Any())
        {
            return 0;
        }
        
        // Filter by origin if specified (targeted/rules)
        if (pathParts.Length > 1)
        {
            if (pathParts[1].Equals(Identifiers.ACTION_TARGETED, StringComparison.OrdinalIgnoreCase))
            {
                allActions = allActions.Where(a => a.IsTargeted).ToList();
            }
            else if (pathParts[1].Equals(Identifiers.ACTION_RULES, StringComparison.OrdinalIgnoreCase))
            {
                allActions = allActions.Where(a => a.IsFromSynchronizationRule).ToList();
            }
            else
            {
                // If it's not targeted/rules, it's an action type
                return FilterActionsByType(allActions, pathParts[1]);
            }
        }
        
        // Filter by action type if specified
        if (pathParts.Length > 2)
        {
            return FilterActionsByType(allActions, pathParts[2]);
        }
        
        return allActions.Count;
    }
    
    private int FilterActionsByType(List<AtomicAction> actions, string actionType)
    {
        var actionTypePattern = new Regex(@"^([a-zA-Z-]+)$", RegexOptions.IgnoreCase);
        var match = actionTypePattern.Match(actionType);
        
        if (!match.Success)
            return 0;
        
        var normalizedActionType = match.Groups[1].Value.ToLowerInvariant();
        
        return normalizedActionType switch
        {
            Identifiers.ACTION_COPY_CONTENTS => actions.Count(a => a.IsSynchronizeContent),
            Identifiers.ACTION_COPY => actions.Count(a => a.IsSynchronizeContentAndDate),
            Identifiers.ACTION_COPY_DATES => actions.Count(a => a.IsSynchronizeDate),
            Identifiers.ACTION_SYNCHRONIZE_DELETE => actions.Count(a => a.IsDelete),
            Identifiers.ACTION_SYNCHRONIZE_CREATE => actions.Count(a => a.IsCreate),
            Identifiers.ACTION_DO_NOTHING => actions.Count(a => a.IsDoNothing),
            _ => 0
        };
    }
}
