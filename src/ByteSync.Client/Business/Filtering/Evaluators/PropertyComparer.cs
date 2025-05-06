using System.Text.RegularExpressions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Business.Filtering.Values;
using ByteSync.Interfaces.Services.Filtering;

namespace ByteSync.Business.Filtering.Evaluators;

public class PropertyComparer : IPropertyComparer
{
    public bool CompareValues(PropertyValueCollection collection1, PropertyValueCollection collection2, ComparisonOperator op)
    {
        if (collection1.Count == 0 && collection2.Count == 0)
            return op == ComparisonOperator.Equals;

        if (collection1.Count == 0 || collection2.Count == 0)
            return op == ComparisonOperator.NotEquals;

        // Try to convert to common type
        if (collection1.CollectionType == PropertyValueType.String && collection2.CollectionType == PropertyValueType.String)
        {
            return CompareStrings(collection1, collection2, op);
        }
        else if (collection1.CollectionType == PropertyValueType.DateTime && collection2.CollectionType == PropertyValueType.DateTime)
        {
            return CompareDateTimes(collection1, collection2, op);
        }
        else if (collection1.CollectionType == PropertyValueType.Numeric && collection2.CollectionType == PropertyValueType.Numeric)
        {
            return CompareNumbers(collection1, collection2, op);
        }
        
        return false;
    }
    private bool CompareStrings(PropertyValueCollection collection1, PropertyValueCollection collection2, ComparisonOperator op)
    {
        foreach (var value1 in collection1)
        {
            var s1 = (value1.Value as string)!;
            
            foreach (var value2 in collection2)
            {
                var s2 = (value2.Value as string)!;

                if (op switch
                    {
                        ComparisonOperator.Equals => string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase),
                        ComparisonOperator.NotEquals => !string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase),
                        ComparisonOperator.GreaterThan => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) > 0,
                        ComparisonOperator.LessThan => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) < 0,
                        ComparisonOperator.GreaterThanOrEqual => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) >= 0,
                        ComparisonOperator.LessThanOrEqual => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) <= 0,
                        ComparisonOperator.RegexMatch => Regex.IsMatch(s1, s2),
                        _ => throw new ArgumentException($"Unsupported string operator: {op}")
                    })
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CompareDateTimes(PropertyValueCollection collection1, PropertyValueCollection collection2, ComparisonOperator op)
    {
        foreach (var value1 in collection1)
        {
            if (value1.Value is not DateTime d1)
                continue;

            foreach (var value2 in collection2)
            {
                if (value2.Value is not DateTime d2)
                    continue;

                if (op switch
                    {
                        ComparisonOperator.Equals => d1 == d2,
                        ComparisonOperator.NotEquals => d1 != d2,
                        ComparisonOperator.GreaterThan => d1 > d2,
                        ComparisonOperator.LessThan => d1 < d2,
                        ComparisonOperator.GreaterThanOrEqual => d1 >= d2,
                        ComparisonOperator.LessThanOrEqual => d1 <= d2,
                        _ => throw new ArgumentException($"Unsupported datetime operator: {op}")
                    })
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CompareNumbers(PropertyValueCollection collection1, PropertyValueCollection collection2, ComparisonOperator op)
    {
        const double epsilon = 1e-8;
        
        foreach (var value1 in collection1)
        {
            var n1 = Convert.ToDouble(value1.Value);

            foreach (var value2 in collection2)
            {
                var n2 = Convert.ToDouble(value2.Value);

                if (op switch
                    {
                        ComparisonOperator.Equals => Math.Abs(n1 - n2) < epsilon, 
                        ComparisonOperator.NotEquals => Math.Abs(n1 - n2) >= epsilon,
                        ComparisonOperator.GreaterThan => n1 > n2,
                        ComparisonOperator.LessThan => n1 < n2,
                        ComparisonOperator.GreaterThanOrEqual => n1 >= n2,
                        ComparisonOperator.LessThanOrEqual => n1 <= n2,
                        _ => throw new ArgumentException($"Unsupported numeric operator: {op}")
                    })
                {
                    return true;
                }
            }
        }

        return false;
    }
}