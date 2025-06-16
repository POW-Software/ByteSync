using ByteSync.Business.Filtering.Parsing;

namespace ByteSync.Business.Filtering.Expressions;

public class NameExpression : BaseElementPathExpression
{
    public NameExpression(string searchText, ComparisonOperator comparisonOperator)
        : base(searchText, comparisonOperator)
    {
    }
}