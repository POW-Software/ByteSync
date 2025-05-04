using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class TextSearchExpressionEvaluator : ExpressionEvaluator<TextSearchExpression>
{
    public override bool Evaluate(TextSearchExpression expression, ComparisonItem item)
    {
        return item.PathIdentity.FileName.Contains(expression.SearchText, StringComparison.OrdinalIgnoreCase);
    }
}