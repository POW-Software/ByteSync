using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class NotExpression : FilterExpression
{
    private readonly FilterExpression _expression;

    public NotExpression(FilterExpression expression)
    {
        _expression = expression;
    }

    public override bool Evaluate(ComparisonItem item)
    {
        return !_expression.Evaluate(item);
    }
}