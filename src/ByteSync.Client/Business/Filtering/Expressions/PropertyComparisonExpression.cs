using System.Text.RegularExpressions;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering.Comparing;
using ByteSync.Business.Filtering.Extensions;
using ByteSync.Business.Filtering.Values;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class PropertyComparisonExpression : FilterExpression
{
    private readonly DataPart _sourceDataPart;
    private readonly string _property;
    private readonly FilterOperator _operator;
    private readonly DataPart? _targetDataPart;
    private readonly string? _targetProperty;
    private readonly string? _targetValue;
    private readonly bool _isDataSourceComparison;

    public PropertyComparisonExpression(DataPart sourceDataPart, string property, FilterOperator @operator, DataPart? targetDataPart,
        string? targetPropertyOrValue = null)
    {
        _sourceDataPart = sourceDataPart;
        _property = property;
        _operator = @operator;
        _targetDataPart = targetDataPart;

        // Determine if this is a comparison between two data sources or a data source and a value
        _isDataSourceComparison = _targetDataPart != null;

        if (_isDataSourceComparison)
        {
            _targetProperty = targetPropertyOrValue;
        }
        else
        {
            _targetValue = targetPropertyOrValue;
        }
    }

    public override bool Evaluate(ComparisonItem item)
    {
        // Get source property value
        var sourceValues = PropertyComparer.GetPropertyValue(item, _sourceDataPart, _property);

        if (sourceValues.Count == 0)
        {
            return _operator == FilterOperator.NotEquals;
        }

        // Handle comparison based on type
        if (_isDataSourceComparison)
        {
            // Compare with another data source property
            var targetValues = PropertyComparer.GetPropertyValue(item, _targetDataPart, _targetProperty ?? _property);
            return PropertyComparer.CompareValues(sourceValues, targetValues, _operator);
        }
        else
        {
            // Compare with a literal value
            return CompareWithLiteral(sourceValues, _targetValue, _operator, _property);
        }
    }

    private bool CompareWithLiteral(List<PropertyValue> sourceValues, string targetValue, FilterOperator op, string property)
    {
        // Handle special case for regex
        if (op == FilterOperator.RegexMatch && sourceValues.Any(sv => sv.Value is string))
        {
            try
            {
                return sourceValues.Any(sv => Regex.IsMatch(sv.Value.ToString()!, targetValue));
            }
            catch (ArgumentException)
            {
                // Invalid regex
                return false;
            }
        }

        // Handle special cases by property type
        var propertyLower = property.ToLowerInvariant();

        if (propertyLower == "size")
        {
            var targetValues = new List<PropertyValue>();
            
            // Parse size with units
            // long size = (long)sourceValue;
            if (targetValue.IndexOfAny(new[] { 'k', 'K', 'm', 'M', 'g', 'G', 't', 'T' }) >= 0)
            {
                try
                {
                    long targetSize = targetValue.ToBytes();
                    // return PropertyComparer.CompareValues(size, targetSize, op);
                    targetValues.Add(new PropertyValue(targetSize));
                }
                catch
                {
                    // return false;
                }
            }
            else
            {
                // Plain number, try to parse
                if (long.TryParse(targetValue, out long targetSize))
                {
                    targetValues.Add(new PropertyValue(targetSize));
                    // return PropertyComparer.CompareValues(size, targetSize, op);
                }

                // return false;
            }
            
            return PropertyComparer.CompareValues(sourceValues, targetValues, op);
        }
        
        return false;
        
        // else if (propertyLower == "date")
        // {
        //     // Parse date
        //     if (DateTime.TryParse(targetValue.Trim('"', '\''), out DateTime targetDate))
        //     {
        //         return PropertyComparer.CompareValues(sourceValues, targetDate, op);
        //     }
        //
        //     return false;
        // }
        // else if (propertyLower == "content" || propertyLower == "contentanddate")
        // {
        //     // Direct comparison for content hashes
        //     return PropertyComparer.CompareValues(sourceValues, targetValue, op);
        // }
        // else
        // {
        //     // Default string comparison
        //     return PropertyComparer.CompareValues(sourceValues, targetValue, op);
        // }
    }
}