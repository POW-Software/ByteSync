using ByteSync.Business.Filtering.Expressions;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class NotExpressionEvaluator : ExpressionEvaluator<NotExpression>
{
    private readonly IExpressionEvaluatorFactory _expressionEvaluatorFactory;

    public NotExpressionEvaluator(IExpressionEvaluatorFactory expressionEvaluatorFactory)
    {
        _expressionEvaluatorFactory = expressionEvaluatorFactory;
    }

    public override bool Evaluate(NotExpression expression, ComparisonItem item)
    {
        var innerEvaluator = _expressionEvaluatorFactory.GetEvaluator(expression.Expression);
        return !innerEvaluator.Evaluate(expression.Expression, item);
    }
}