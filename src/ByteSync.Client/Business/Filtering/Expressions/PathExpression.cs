using ByteSync.Business.Filtering.Parsing;

namespace ByteSync.Business.Filtering.Expressions;

public class PathExpression : BaseElementPathExpression
{
    public PathExpression(string searchText, ComparisonOperator comparisonOperator)
        : base(searchText, comparisonOperator)
    {
    }
}