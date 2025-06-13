using ByteSync.Business.Filtering.Parsing;

namespace ByteSync.Business.Filtering.Expressions;

public class NameExpression : FilterExpression
{
    public string SearchText { get; }
    
    public ComparisonOperator ComparisonOperator { get; }

    public NameExpression(string searchText, ComparisonOperator comparisonOperator)
    {
        SearchText = searchText;
        ComparisonOperator = comparisonOperator;
    }
}