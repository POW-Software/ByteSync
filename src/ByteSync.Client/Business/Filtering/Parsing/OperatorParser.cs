using ByteSync.Interfaces.Services.Filtering;

namespace ByteSync.Business.Filtering.Parsing;

public class OperatorParser : IOperatorParser
{
    public ComparisonOperator Parse(string operatorString)
    {
        return operatorString switch
        {
            "==" => ComparisonOperator.Equals,
            "=" => ComparisonOperator.Equals,
            "!=" => ComparisonOperator.NotEquals,
            "<>" => ComparisonOperator.NotEquals,
            ">" => ComparisonOperator.GreaterThan,
            "<" => ComparisonOperator.LessThan,
            ">=" => ComparisonOperator.GreaterThanOrEqual,
            "<=" => ComparisonOperator.LessThanOrEqual,
            "=~" => ComparisonOperator.RegexMatch,
            // "&&" => FilterOperator.And,
            // "||" => FilterOperator.Or,
            _ => throw new ArgumentException($"Unknown operator: {operatorString}")
        };
    }
}