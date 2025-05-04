using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class OnlyExpressionEvaluator : ExpressionEvaluator<OnlyExpression>
{
    public override bool Evaluate(OnlyExpression expression, ComparisonItem item)
    {
        var inventories = item.ContentIdentities
            .SelectMany(ci => ci.GetInventories())
            .ToHashSet();

        return inventories.Count == 1 && inventories.First().Letter.Equals(expression.DataSource, StringComparison.OrdinalIgnoreCase);
    }
}