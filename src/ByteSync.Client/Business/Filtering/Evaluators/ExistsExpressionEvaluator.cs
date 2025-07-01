using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class ExistsExpressionEvaluator : ExpressionEvaluator<ExistsExpression>
{
    public override bool Evaluate(ExistsExpression expression, ComparisonItem item)
    {
        var inventories = item.ContentIdentities
            .SelectMany(ci => ci.GetInventories())
            .ToHashSet();
        
        var inventoryParts = item.ContentIdentities
            .SelectMany(ci => ci.GetInventoryParts())
            .ToHashSet();
        
        List<string> codes = inventories
            .Select(i => i.Code)
            .Union(inventoryParts.Select(i => i.Code))
            .ToList();

        return codes.Any(c => c.Equals(expression.DataSource, StringComparison.OrdinalIgnoreCase));
    }
}