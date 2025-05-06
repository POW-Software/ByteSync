using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class TrueExpressionEvaluator : ExpressionEvaluator<TrueExpression>
{
    public override bool Evaluate(TrueExpression expression, ComparisonItem item) => true;
}