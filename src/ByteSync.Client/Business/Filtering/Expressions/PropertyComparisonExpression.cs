using System.Text.RegularExpressions;
using ByteSync.Business.Filtering.Comparing;
using ByteSync.Business.Filtering.Extensions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class PropertyComparisonExpression : FilterExpression
{
    private readonly string _sourceDataSource;
    private readonly string _property;
    private readonly string _operator;
    private readonly string _targetDataSource;
    private readonly string? _targetProperty;
    private readonly string? _targetValue;
    private readonly bool _isDataSourceComparison;

    public PropertyComparisonExpression(string sourceDataSource, string property, string @operator, string targetDataSource,
        string? targetPropertyOrValue = null)
    {
        _sourceDataSource = sourceDataSource;
        _property = property;
        _operator = @operator;
        _targetDataSource = targetDataSource;

        // Determine if this is a comparison between two data sources or a data source and a value
        _isDataSourceComparison = targetDataSource != null;

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
        object sourceValue = PropertyComparer.GetPropertyValue(item, _sourceDataSource, _property);

        if (sourceValue == null)
        {
            return _operator == "!=" || _operator == "<>";
        }

        // Handle comparison based on type
        if (_isDataSourceComparison)
        {
            // Compare with another data source property
            object targetValue = PropertyComparer.GetPropertyValue(item, _targetDataSource, _targetProperty ?? _property);
            return PropertyComparer.CompareValues(sourceValue, targetValue, _operator);
        }
        else
        {
            // Compare with a literal value
            return CompareWithLiteral(sourceValue, _targetValue, _operator, _property);
        }
    }

    private bool CompareWithLiteral(object sourceValue, string targetValue, string op, string property)
    {
        // Handle special case for regex
        if (op == "=~" && sourceValue is string sourceString)
        {
            try
            {
                return Regex.IsMatch(sourceString, targetValue);
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
            // Parse size with units
            long size = (long)sourceValue;
            if (targetValue.IndexOfAny(new[] { 'k', 'K', 'm', 'M', 'g', 'G', 't', 'T' }) >= 0)
            {
                try
                {
                    long targetSize = targetValue.ToBytes();
                    return PropertyComparer.CompareValues(size, targetSize, op);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                // Plain number, try to parse
                if (long.TryParse(targetValue, out long targetSize))
                {
                    return PropertyComparer.CompareValues(size, targetSize, op);
                }

                return false;
            }
        }
        else if (propertyLower == "date")
        {
            // Parse date
            if (DateTime.TryParse(targetValue.Trim('"', '\''), out DateTime targetDate))
            {
                return PropertyComparer.CompareValues(sourceValue, targetDate, op);
            }

            return false;
        }
        else if (propertyLower == "content" || propertyLower == "contentanddate")
        {
            // Direct comparison for content hashes
            return PropertyComparer.CompareValues(sourceValue, targetValue, op);
        }
        else
        {
            // Default string comparison
            return PropertyComparer.CompareValues(sourceValue, targetValue, op);
        }
    }
}