using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class OnlyExpressionEvaluator : ExpressionEvaluator<OnlyExpression>
{
    public override bool Evaluate(OnlyExpression expression, ComparisonItem item)
    {
        bool result;
        
        if (expression.DataSource.Length == 1)
        {
            var inventories = item.ContentIdentities
                .SelectMany(ci => ci.GetInventories())
                .ToHashSet();

            result = inventories.Count == 1 && inventories.First().Letter.Equals(expression.DataSource, StringComparison.OrdinalIgnoreCase);
        }
        else if (expression.DataSource.Length > 1)
        {
            var inventoryParts = item.ContentIdentities
                .SelectMany(ci => ci.GetInventoryParts())
                .ToHashSet();
            
            result = inventoryParts.Count == 1 && inventoryParts.First().Code.Equals(expression.DataSource, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            result = false;
        }

        return result;
    }
}