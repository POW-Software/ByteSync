using ByteSync.Business.Filtering.Expressions;

namespace ByteSync.Business.Filtering.Parsing;

public class ParseResult
{
    public bool IsComplete { get; set; }
    public FilterExpression? Expression { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static ParseResult Incomplete(string message) => 
        new ParseResult { IsComplete = false, ErrorMessage = message };
        
    public static ParseResult Success(FilterExpression expression) => 
        new ParseResult { IsComplete = true, Expression = expression };
}