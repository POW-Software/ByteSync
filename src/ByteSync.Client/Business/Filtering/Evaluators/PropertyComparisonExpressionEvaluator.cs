using System.Text.RegularExpressions;
using ByteSync.Business.Filtering.Comparing;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Extensions;
using ByteSync.Business.Filtering.Values;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class PropertyComparisonExpressionEvaluator : ExpressionEvaluator<PropertyComparisonExpression>
{
    public override bool Evaluate(PropertyComparisonExpression expression, ComparisonItem item)
    {
        // Get source property value
        var sourceValues = PropertyComparer.GetPropertyValue(item, expression.SourceDataPart, expression.Property);

        if (sourceValues.Count == 0)
        {
            return expression.Operator == FilterOperator.NotEquals;
        }

        // Handle comparison based on type
        if (expression.IsDataSourceComparison)
        {
            // Compare with another data source property
            var targetValues = PropertyComparer.GetPropertyValue(item, expression.TargetDataPart, expression.TargetProperty ?? expression.Property);
            return PropertyComparer.CompareValues(sourceValues, targetValues, expression.Operator);
        }
        else
        {
            // Compare with a literal value
            return CompareWithLiteral(sourceValues, expression.TargetValue, expression.Operator, expression.Property);
        }
    }

    private bool CompareWithLiteral(PropertyValueCollection sourceValues, string targetValue, FilterOperator op, string property)
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
            var targetValues = new PropertyValueCollection();
            
            // Parse size with units
            if (targetValue.IndexOfAny(new[] { 'k', 'K', 'm', 'M', 'g', 'G', 't', 'T' }) >= 0)
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
            
            return PropertyComparer.CompareValues(sourceValues, targetValues, op);
        }
        
        return false;
    }
}