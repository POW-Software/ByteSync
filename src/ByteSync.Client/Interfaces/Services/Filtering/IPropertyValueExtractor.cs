using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering.Values;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Interfaces.Services.Filtering;

public interface IPropertyValueExtractor
{
    PropertyValueCollection GetPropertyValue(ComparisonItem item, DataPart? dataPart, string property);
}