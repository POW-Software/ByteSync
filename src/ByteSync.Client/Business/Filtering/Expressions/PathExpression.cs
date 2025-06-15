using ByteSync.Business.Filtering.Parsing;

namespace ByteSync.Business.Filtering.Expressions;

public class PathExpression : FilterExpression
{
    public string SearchText { get; }
    
    public ComparisonOperator ComparisonOperator { get; }

    public PathExpression(string searchText, ComparisonOperator comparisonOperator)
    {
        SearchText = searchText;
        ComparisonOperator = comparisonOperator;
    }
}