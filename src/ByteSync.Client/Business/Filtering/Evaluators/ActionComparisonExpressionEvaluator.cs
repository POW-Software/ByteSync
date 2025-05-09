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
        // Extraire les parties du chemin d'action (actions, targeted/rules, type d'action)
        string[] pathParts = expression.ActionPath.Split('.');
        
        int actionCount = GetActionCount(item, pathParts);
        
        // Comparer avec l'opérateur spécifié
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
        // Base du chemin est toujours "actions"
        if (pathParts.Length == 0 || pathParts[0] != "actions")
        {
            return 0;
        }
            
        // Obtenir la liste complète des actions
        var allActions = _actionRepository.GetAtomicActions(item);

        if (!allActions.Any())
        {
            return 0;
        }
        
        // Filtrer par origine si spécifiée (targeted/rules)
        if (pathParts.Length > 1)
        {
            if (pathParts[1].Equals("targeted", StringComparison.OrdinalIgnoreCase))
            {
                allActions = allActions.Where(a => a.IsTargeted).ToList();
            }
            else if (pathParts[1].Equals("rules", StringComparison.OrdinalIgnoreCase))
            {
                allActions = allActions.Where(a => a.IsFromSynchronizationRule).ToList();
            }
            else
            {
                // Si ce n'est pas targeted/rules, c'est un type d'action
                return FilterActionsByType(allActions, pathParts[1]);
            }
        }
        
        // Filtrer par type d'action si spécifié
        if (pathParts.Length > 2)
        {
            return FilterActionsByType(allActions, pathParts[2]);
        }
        
        return allActions.Count;
    }
    
    private int FilterActionsByType(List<AtomicAction> actions, string actionType)
    {
        var actionTypePattern = new Regex(@"^([a-zA-Z]+)$", RegexOptions.IgnoreCase);
        var match = actionTypePattern.Match(actionType);
        
        if (!match.Success)
            return 0;
        
        var normalizedActionType = match.Groups[1].Value.ToLowerInvariant();
        
        return normalizedActionType switch
        {
            "synchronizecontent" => actions.Count(a => a.IsSynchronizeContent),
            "synchronizecontentonly" => actions.Count(a => a.IsSynchronizeContentOnly),
            "synchronizecontentanddate" => actions.Count(a => a.IsSynchronizeContentAndDate),
            "synchronizedate" => actions.Count(a => a.IsSynchronizeDate),
            "delete" => actions.Count(a => a.IsDelete),
            "create" => actions.Count(a => a.IsCreate),
            "donothing" => actions.Count(a => a.IsDoNothing),
            _ => 0
        };
    }
}