using ByteSync.Business.Filtering.Parsing;

namespace ByteSync.Business.Filtering.Expressions;

public abstract class BaseElementPathExpression : FilterExpression
{
    public string SearchText { get; }
    
    public ComparisonOperator ComparisonOperator { get; }

    protected BaseElementPathExpression(string searchText, ComparisonOperator comparisonOperator)
    {
        SearchText = searchText;
        ComparisonOperator = comparisonOperator;
    }
}