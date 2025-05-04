using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class NotExpressionEvaluator : ExpressionEvaluator<NotExpression>
{
    private readonly ExpressionEvaluatorFactory _evaluatorFactory;

    public NotExpressionEvaluator(ExpressionEvaluatorFactory evaluatorFactory)
    {
        _evaluatorFactory = evaluatorFactory;
    }

    public override bool Evaluate(NotExpression expression, ComparisonItem item)
    {
        var innerEvaluator = _evaluatorFactory.GetEvaluator(expression.Expression);
        return !innerEvaluator.Evaluate(expression.Expression, item);
    }
}