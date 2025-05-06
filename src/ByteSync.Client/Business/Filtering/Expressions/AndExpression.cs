using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class AndExpression : FilterExpression
{
    public FilterExpression Left { get; }
    public FilterExpression Right { get; }

    public AndExpression(FilterExpression left, FilterExpression right)
    {
        Left = left;
        Right = right;
    }
}
