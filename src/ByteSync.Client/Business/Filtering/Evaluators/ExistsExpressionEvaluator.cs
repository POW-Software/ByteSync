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

        return inventories.Any(i => i.Letter.Equals(expression.DataSource, StringComparison.OrdinalIgnoreCase));
    }
}