using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Parsing;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IFilterParser
{
    ParseResult TryParse(string filterText);
    
    [System.Obsolete("Use TryParse instead to handle incomplete inputs gracefully")]
    FilterExpression Parse(string filterText);
}