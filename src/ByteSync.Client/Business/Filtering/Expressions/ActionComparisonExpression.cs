using ByteSync.Business.Filtering.Parsing;

namespace ByteSync.Business.Filtering.Expressions;

public class ActionComparisonExpression : FilterExpression
{
    public string ActionPath { get; }
    public ComparisonOperator Operator { get; }
    public int Value { get; }

    public ActionComparisonExpression(string actionPath, ComparisonOperator @operator, int value)
    {
        ActionPath = actionPath;
        Operator = @operator;
        Value = value;
    }
}