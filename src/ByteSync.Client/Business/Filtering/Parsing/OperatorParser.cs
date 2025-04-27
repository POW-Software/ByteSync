using ByteSync.Interfaces.Services.Filtering;

namespace ByteSync.Business.Filtering.Parsing;

public class OperatorParser : IOperatorParser
{
    public FilterOperator Parse(string operatorString)
    {
        return operatorString switch
        {
            "==" => FilterOperator.Equals,
            "=" => FilterOperator.Equals,
            "!=" => FilterOperator.NotEquals,
            "<>" => FilterOperator.NotEquals,
            ">" => FilterOperator.GreaterThan,
            "<" => FilterOperator.LessThan,
            ">=" => FilterOperator.GreaterThanOrEqual,
            "<=" => FilterOperator.LessThanOrEqual,
            "=~" => FilterOperator.RegexMatch,
            // "&&" => FilterOperator.And,
            // "||" => FilterOperator.Or,
            _ => throw new ArgumentException($"Unknown operator: {operatorString}")
        };
    }
}