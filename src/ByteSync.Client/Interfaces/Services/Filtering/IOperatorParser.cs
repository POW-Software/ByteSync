using ByteSync.Business.Filtering;
using ByteSync.Business.Filtering.Parsing;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IOperatorParser
{
    ComparisonOperator Parse(string operatorString);
}