using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class FutureStateExpression : FilterExpression
{
    public FilterExpression BaseExpression { get; }

    public FutureStateExpression(FilterExpression baseExpression)
    {
        BaseExpression = baseExpression;
    }
}