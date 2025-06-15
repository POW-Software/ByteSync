using System.Text.RegularExpressions;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Extensions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Business.Filtering.Values;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class PropertyComparisonExpressionEvaluator : ExpressionEvaluator<PropertyComparisonExpression>
{
    private readonly IPropertyValueExtractor _propertyValueExtractor;
    private readonly IPropertyComparer _propertyComparer;

    public PropertyComparisonExpressionEvaluator(IPropertyValueExtractor propertyValueExtractor, IPropertyComparer propertyComparer)
    {
        _propertyValueExtractor = propertyValueExtractor;
        _propertyComparer = propertyComparer;
    }
    
    public override bool Evaluate(PropertyComparisonExpression expression, ComparisonItem item)
    {
        // Get source property value
        var sourceValues = _propertyValueExtractor.GetPropertyValue(item, expression.SourceDataPart, expression.Property);

        if (sourceValues.Count == 0)
        {
            return expression.Operator == ComparisonOperator.NotEquals;
        }

        // Handle comparison based on type
        if (expression.IsDataSourceComparison)
        {
            // Compare with another data source property
            var targetValues = _propertyValueExtractor.GetPropertyValue(item, expression.TargetDataPart, expression.TargetProperty ?? expression.Property);
            return _propertyComparer.CompareValues(sourceValues, targetValues, expression.Operator);
        }
        else
        {
            // Compare with a literal value
            return CompareWithLiteral(sourceValues, expression.TargetValue, expression.Operator, expression.Property);
        }
    }

    private bool CompareWithLiteral(PropertyValueCollection sourceValues, string targetValue, ComparisonOperator op, string property)
    {
        // Handle special case for regex
        if (op == ComparisonOperator.RegexMatch && sourceValues.Any(sv => sv.Value is string))
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

        if (propertyLower == Identifiers.PROPERTY_SIZE)
        {
            var targetValues = new PropertyValueCollection();
            
            // Parse size with units
            if (targetValue.IndexOfAny(new[] { 'k', 'K', 'm', 'M', 'g', 'G', 't', 'T', 'b' }) >= 0)
            {
                try
                {
                    long targetSize = targetValue.ToBytes();
                    targetValues.Add(new PropertyValue(targetSize));
                }
                catch
                {
                    // Unable to parse
                }
            }
            else
            {
                // Plain number, try to parse
                if (long.TryParse(targetValue, out long targetSize))
                {
                    targetValues.Add(new PropertyValue(targetSize));
                }
            }
            
            return _propertyComparer.CompareValues(sourceValues, targetValues, op);
        }
        else if (propertyLower == Identifiers.PROPERTY_LAST_WRITE_TIME)
        {
            var targetValues = new PropertyValueCollection();
            DateTime targetDateTime;

            if (targetValue.StartsWith("now-", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    var duration = targetValue.Substring(4); // Extract the part after “now-”
                    var now = DateTime.UtcNow;

                    // Identify unit and calculate target date
                    var unit = duration.Last();
                    var value = int.Parse(duration[..^1]); // Retrieve numeric value

                    targetDateTime = unit switch
                    {
                        'm' => now.AddMinutes(-value),
                        'h' => now.AddHours(-value),
                        'd' => now.AddDays(-value),
                        'w' => now.AddDays(-value * 7),
                        'M' => now.AddMonths(-value),
                        'y' => now.AddYears(-value),
                        _ => throw new FormatException("Unité non reconnue.")
                    };

                    targetValues.Add(new PropertyValue(targetDateTime));
                }
                catch
                {
                    // Invalid Format
                    return false;
                }
            }
            else if (DateTime.TryParseExact(targetValue, new[] { "yyyy-MM-dd", "yyyy-MM-dd-HH-mm-ss" }, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out  targetDateTime))
            {
                targetValues.Add(new PropertyValue(targetDateTime));
            }
            else
            {
                // Invalid DateTime format
                return false;
            }

            return _propertyComparer.CompareValues(sourceValues, targetValues, op);
        }
        
        return false;
    }
}