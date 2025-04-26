using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class AndExpression : FilterExpression
{
    private readonly FilterExpression _left;
    private readonly FilterExpression _right;

    public AndExpression(FilterExpression left, FilterExpression right)
    {
        _left = left;
        _right = right;
    }

    public override bool Evaluate(ComparisonItem item)
    {
        return _left.Evaluate(item) && _right.Evaluate(item);
    }
}
