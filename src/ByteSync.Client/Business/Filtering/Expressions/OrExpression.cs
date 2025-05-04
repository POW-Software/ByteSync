using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class OrExpression : FilterExpression
{
    public FilterExpression Left { get; }
    public FilterExpression Right { get; }

    public OrExpression(FilterExpression left, FilterExpression right)
    {
        Left = left;
        Right = right;
    }
}