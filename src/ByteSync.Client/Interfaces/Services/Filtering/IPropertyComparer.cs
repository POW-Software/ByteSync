using ByteSync.Business.Filtering;
using ByteSync.Business.Filtering.Values;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IPropertyComparer
{
    bool CompareValues(PropertyValueCollection collection1, PropertyValueCollection collection2, FilterOperator filterOperator);
}