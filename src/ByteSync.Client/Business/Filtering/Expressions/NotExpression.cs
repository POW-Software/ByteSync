using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class NotExpression : FilterExpression
{
    public FilterExpression Expression { get; }

    public NotExpression(FilterExpression expression)
    {
        Expression = expression;
    }
}