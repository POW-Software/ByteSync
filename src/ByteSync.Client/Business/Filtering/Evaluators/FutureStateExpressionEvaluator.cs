using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class FutureStateExpressionEvaluator : ExpressionEvaluator<FutureStateExpression>
{
    private readonly ExpressionEvaluatorFactory _evaluatorFactory;

    public FutureStateExpressionEvaluator(ExpressionEvaluatorFactory evaluatorFactory)
    {
        _evaluatorFactory = evaluatorFactory;
    }

    public override bool Evaluate(FutureStateExpression expression, ComparisonItem item)
    {
        // This is a placeholder implementation
        // The actual implementation would need to predict future state based on actions
        // For now, we'll just return the current state
        var baseEvaluator = _evaluatorFactory.GetEvaluator(expression.BaseExpression);
        return baseEvaluator.Evaluate(expression.BaseExpression, item);
    }
}