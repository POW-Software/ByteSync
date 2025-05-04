using System.Text.RegularExpressions;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering.Comparing;
using ByteSync.Business.Filtering.Extensions;
using ByteSync.Business.Filtering.Values;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class PropertyComparisonExpression : FilterExpression
{
    public DataPart SourceDataPart { get; }
    public string Property { get; }
    public FilterOperator Operator { get; }
    public DataPart? TargetDataPart { get; }
    public string? TargetProperty { get; private set; }
    public string? TargetValue { get; private set; }
    public bool IsDataSourceComparison { get; }

    public PropertyComparisonExpression(DataPart sourceDataPart, string property, FilterOperator @operator, 
        DataPart? targetDataPart, string? targetPropertyOrValue = null)
    {
        SourceDataPart = sourceDataPart;
        Property = property;
        Operator = @operator;
        TargetDataPart = targetDataPart;

        // Determine if this is a comparison between two data sources or a data source and a value
        IsDataSourceComparison = TargetDataPart != null;

        if (IsDataSourceComparison)
        {
            TargetProperty = targetPropertyOrValue;
        }
        else
        {
            TargetValue = targetPropertyOrValue;
        }
    }
}