using System.Text.RegularExpressions;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering.Values;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;

namespace ByteSync.Business.Filtering.Comparing;

public class PropertyComparer
{
    /// <summary>
    /// Gets property value from a ComparisonItem for a specific DataSource
    /// </summary>
    public static List<PropertyValue> GetPropertyValue(ComparisonItem item, DataPart? dataPart, string property)
    {
        if (dataPart == null)
        {
            return GetGeneralPropertyValue(item, property);
        }

        // Find content identities for the specific data source
        var contentIdentities = item.GetContentIdentities(dataPart.GetApplicableInventoryPart());
        
        // var inventories = item.ContentIdentities
        //     .SelectMany(ci => ci.GetInventories())
        //     .Where(i => i.Letter.Equals(dataSource, StringComparison.OrdinalIgnoreCase))
        //     .ToList();
        //
        // if (!inventories.Any())
        // {
        //     return null; // Data source not found
        // }

        var propertyLower = property.ToLowerInvariant();

        switch (propertyLower)
        {
            case "content":
                return ExtractContent(contentIdentities);
                
            
            // case "content":
            //     return GetContent(item, inventories[0]);
            // case "contentanddate":
            //     return GetContentAndDate(item, inventories[0]);
            // case "size":
            //     return GetSize(item, inventories[0]);
            // case "date":
            //     return GetDate(item, inventories[0]);
            // case "ext":
            //     return System.IO.Path.GetExtension(item.PathIdentity.FileName).TrimStart('.');
            // case "name":
            //     return System.IO.Path.GetFileNameWithoutExtension(item.PathIdentity.FileName);
            // case "path":
            //     return item.PathIdentity.FileName;
            default:
                throw new ArgumentException($"Unknown property: {property}");
        }
    }

    private static List<PropertyValue> ExtractContent(List<ContentIdentity> contentIdentities)
    {
        var contents = new HashSet<string>();

        foreach (var contentIdentity in contentIdentities)
        {
            contents.Add(contentIdentity.Core!.SignatureHash!);
        }
        
        var result = new List<PropertyValue>();
        foreach (var content in contents)
        {
            result.Add(new PropertyValue(content));
        }

        return result;
    }

    /// <summary>
    /// Gets property value that's not specific to a data source
    /// </summary>
    private static List<PropertyValue> GetGeneralPropertyValue(ComparisonItem item, string property)
    {
        var propertyLower = property.ToLowerInvariant();

        object value;
        switch (propertyLower)
        {
            case "ext":
                value = System.IO.Path.GetExtension(item.PathIdentity.FileName).TrimStart('.');
                break;
            case "name":
                value = System.IO.Path.GetFileNameWithoutExtension(item.PathIdentity.FileName);
                break;
            case "path":
                value = item.PathIdentity.FileName;
                break;
            default:
                throw new ArgumentException($"Property '{property}' requires a data source");
        }
        
        var result = new List<PropertyValue>();
        result.Add(new PropertyValue(result));
        
        return result;
    }

    /*
    /// <summary>
    /// Gets content for an inventory
    /// </summary>
    private static object GetContent(ComparisonItem item, Inventory inventory)
    {
        var contentIdentity = item.ContentIdentities
            .FirstOrDefault(ci => ci.GetInventories().Any(i => i.Equals(inventory)));

        return contentIdentity?.Core.ContentHash;
    }

    /// <summary>
    /// Gets content and date for an inventory
    /// </summary>
    private static object GetContentAndDate(ComparisonItem item, Inventory inventory)
    {
        var contentIdentity = item.ContentIdentities
            .FirstOrDefault(ci => ci.GetInventories().Any(i => i.Equals(inventory)));

        if (contentIdentity == null)
            return null;

        return $"{contentIdentity.Core.ContentHash}_{contentIdentity.Core.LastWriteTimeUtc.Ticks}";
    }

    /// <summary>
    /// Gets size for an inventory
    /// </summary>
    private static object GetSize(ComparisonItem item, Inventory inventory)
    {
        var contentIdentity = item.ContentIdentities
            .FirstOrDefault(ci => ci.GetInventories().Any(i => i.Equals(inventory)));

        return contentIdentity?.Core.Size;
    }

    /// <summary>
    /// Gets last write date for an inventory
    /// </summary>
    private static object GetDate(ComparisonItem item, Inventory inventory)
    {
        var contentIdentity = item.ContentIdentities
            .FirstOrDefault(ci => ci.GetInventories().Any(i => i.Equals(inventory)));

        return contentIdentity?.Core.LastWriteTimeUtc;
    }
    */

    /// <summary>
    /// Compare two property values using the specified operator
    /// </summary>
    public static bool CompareValues(List<PropertyValue> value1, List<PropertyValue> value2, string op)
    {
        if (value1 == null && value2 == null)
            return op == "==" || op == "=";

        if (value1 == null || value2 == null)
            return op == "!=" || op == "<>";

        // Try to convert to common type
        if (value1 is string s1 && value2 is string s2)
        {
            return CompareStrings(s1, s2, op);
        }
        else if (value1 is DateTime d1 && value2 is DateTime d2)
        {
            return CompareDateTimes(d1, d2, op);
        }
        else if (IsNumeric(value1) && IsNumeric(value2))
        {
            return CompareNumbers(Convert.ToDouble(value1), Convert.ToDouble(value2), op);
        }

        // Fall back to string comparison
        return CompareStrings(value1.ToString(), value2.ToString(), op);
    }

    private static bool IsNumeric(object value)
    {
        return value is sbyte || value is byte || value is short || value is ushort ||
               value is int || value is uint || value is long || value is ulong ||
               value is float || value is double || value is decimal;
    }

    private static bool CompareStrings(string s1, string s2, string op)
    {
        return op switch
        {
            "==" or "=" => s1.Equals(s2, StringComparison.OrdinalIgnoreCase),
            "!=" or "<>" => !s1.Equals(s2, StringComparison.OrdinalIgnoreCase),
            ">" => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) > 0,
            "<" => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) < 0,
            ">=" => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) >= 0,
            "<=" => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) <= 0,
            "=~" => Regex.IsMatch(s1, s2),
            _ => throw new ArgumentException($"Unsupported string operator: {op}")
        };
    }

    private static bool CompareDateTimes(DateTime d1, DateTime d2, string op)
    {
        return op switch
        {
            "==" or "=" => d1 == d2,
            "!=" or "<>" => d1 != d2,
            ">" => d1 > d2,
            "<" => d1 < d2,
            ">=" => d1 >= d2,
            "<=" => d1 <= d2,
            _ => throw new ArgumentException($"Unsupported datetime operator: {op}")
        };
    }

    private static bool CompareNumbers(double n1, double n2, string op)
    {
        return op switch
        {
            "==" or "=" => Math.Abs(n1 - n2) < 0.0000001, // Use epsilon for floating point comparison
            "!=" or "<>" => Math.Abs(n1 - n2) >= 0.0000001,
            ">" => n1 > n2,
            "<" => n1 < n2,
            ">=" => n1 >= n2,
            "<=" => n1 <= n2,
            _ => throw new ArgumentException($"Unsupported numeric operator: {op}")
        };
    }
}