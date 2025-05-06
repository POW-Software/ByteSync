using ByteSync.Business.Filtering.Expressions;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IFilterParser
{
    FilterExpression Parse(string filterText);
}