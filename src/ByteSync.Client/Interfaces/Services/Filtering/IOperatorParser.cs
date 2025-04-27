using ByteSync.Business.Filtering;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IOperatorParser
{
    FilterOperator Parse(string operatorString);
}