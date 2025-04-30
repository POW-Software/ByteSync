using System.Text.RegularExpressions;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Business.Filtering.Values;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;

namespace ByteSync.Business.Filtering.Comparing;

public class PropertyComparer
{
    /// <summary>
    /// Gets property value from a ComparisonItem for a specific DataSource
    /// </summary>
    public static PropertyValueCollection GetPropertyValue(ComparisonItem item, DataPart? dataPart, string property)
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
        
        var propertyActions = new Dictionary<string, Func<PropertyValueCollection>>
        {
            { nameof(PropertyType.Content).ToLowerInvariant(), () => ExtractContent(contentIdentities) },
            { nameof(PropertyType.ContentAndDate).ToLowerInvariant(), () => ExtractContentAndDate(contentIdentities, dataPart) },
            { nameof(PropertyType.Size).ToLowerInvariant(), () => ExtractSize(contentIdentities, dataPart) },
            { nameof(PropertyType.LastWriteTime).ToLowerInvariant(), () => ExtractLastWriteTime(contentIdentities, dataPart) }
        };

        if (propertyActions.TryGetValue(propertyLower, out var action))
        {
            return action();
        }

        throw new ArgumentException($"Unknown property: {property}");
        

        // switch (propertyLower)
        // {
        //     case Enum.GetName(PropertyType.Content).Tolower():
        //         return ExtractContent(contentIdentities);
        //     case "contentanddate":
        //         return ExtractContentAndDate(contentIdentities, dataPart);
        //     case "size":
        //         return ExtractSize(contentIdentities, dataPart);
        //     
        //     // case "content":
        //     //     return GetContent(item, inventories[0]);
        //     // case "contentanddate":
        //     //     return GetContentAndDate(item, inventories[0]);
        //     // case "size":
        //     //     return GetSize(item, inventories[0]);
        //     // case "date":
        //     //     return GetDate(item, inventories[0]);
        //     // case "ext":
        //     //     return System.IO.Path.GetExtension(item.PathIdentity.FileName).TrimStart('.');
        //     // case "name":
        //     //     return System.IO.Path.GetFileNameWithoutExtension(item.PathIdentity.FileName);
        //     // case "path":
        //     //     return item.PathIdentity.FileName;
        //     default:
        //         throw new ArgumentException($"Unknown property: {property}");
        // }
    }

    private static PropertyValueCollection ExtractContent(List<ContentIdentity> contentIdentities)
    {
        var contents = new HashSet<string>();

        foreach (var contentIdentity in contentIdentities)
        {
            contents.Add(contentIdentity.Core!.SignatureHash!);
        }

        var result = new PropertyValueCollection();
        foreach (var content in contents)
        {
            result.Add(new PropertyValue(content));
        }

        return result;
    }
    
    private static PropertyValueCollection ExtractContentAndDate(List<ContentIdentity> contentIdentities, DataPart dataPart)
    {
        var contents = new HashSet<string>();

        foreach (var contentIdentity in contentIdentities)
        {
            var signatureHash = contentIdentity.Core!.SignatureHash;
            var lastWriteTime = contentIdentity.GetLastWriteTimeUtc(dataPart.GetApplicableInventoryPart());
            
            contents.Add($"{signatureHash}_{lastWriteTime?.Ticks}");
            
            // foreach (var fileSystemDescription in contentIdentity.GetFileSystemDescriptions(dataPart.GetApplicableInventoryPart()))
            // {
            //     if (fileSystemDescription is FileDescription fileDescription)
            //     {
            //         var hash = fileDescription.None_;
            //         var lastWriteTime = contentIdentity.GetLastWriteTimeUtc(dataPart.GetApplicableInventoryPart());
            //         
            //         contents.Add($"{hash}_{lastWriteTime?.Ticks}");
            //     }
            // }
            
            // // var signatureHash = contentIdentity.Core!.SignatureHash;
            // var signatureHash = contentIdentity.Core!.SignatureHash;
            // var lastWriteTime = contentIdentity.GetLastWriteTimeUtc(dataPart.GetApplicableInventoryPart());
            //
            // contents.Add($"{signatureHash}_{lastWriteTime?.Ticks}");
        }

        var result = new PropertyValueCollection();
        foreach (var content in contents)
        {
            result.Add(new PropertyValue(content));
        }

        return result;
    }
    
    private static PropertyValueCollection ExtractSize(List<ContentIdentity> contentIdentities, DataPart dataPart)
    {
        var contents = new HashSet<long>();

        foreach (var contentIdentity in contentIdentities)
        {
            foreach (var fileSystemDescription in contentIdentity.GetFileSystemDescriptions(dataPart.GetApplicableInventoryPart()))
            {
                if (fileSystemDescription is FileDescription fileDescription)
                {
                    contents.Add(fileDescription.Size);
                }
            }
            
            // // var signatureHash = contentIdentity.Core!.SignatureHash;
            // var signatureHash = contentIdentity.Core!.SignatureHash;
            // var lastWriteTime = contentIdentity.GetLastWriteTimeUtc(dataPart.GetApplicableInventoryPart());
            //
            // contents.Add($"{signatureHash}_{lastWriteTime?.Ticks}");
        }

        var result = new PropertyValueCollection();
        foreach (var content in contents)
        {
            result.Add(new PropertyValue(content));
        }

        return result;
    }
    
    private static PropertyValueCollection ExtractLastWriteTime(List<ContentIdentity> contentIdentities, DataPart dataPart)
    {
        var contents = new HashSet<DateTime>();

        foreach (var contentIdentity in contentIdentities)
        {
            foreach (var fileSystemDescription in contentIdentity.GetFileSystemDescriptions(dataPart.GetApplicableInventoryPart()))
            {
                if (fileSystemDescription is FileDescription fileDescription)
                {
                    contents.Add(fileDescription.LastWriteTimeUtc);
                }
            }
            
            // // var signatureHash = contentIdentity.Core!.SignatureHash;
            // var signatureHash = contentIdentity.Core!.SignatureHash;
            // var lastWriteTime = contentIdentity.GetLastWriteTimeUtc(dataPart.GetApplicableInventoryPart());
            //
            // contents.Add($"{signatureHash}_{lastWriteTime?.Ticks}");
        }

        var result = new PropertyValueCollection();
        foreach (var content in contents)
        {
            result.Add(new PropertyValue(content));
        }

        return result;
    }

    /// <summary>
    /// Gets property value that's not specific to a data source
    /// </summary>
    private static PropertyValueCollection GetGeneralPropertyValue(ComparisonItem item, string property)
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

        var result = new PropertyValueCollection();
        result.Add(new PropertyValue(value));
        
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
    public static bool CompareValues(PropertyValueCollection collection1, PropertyValueCollection collection2, FilterOperator op)
    {
        if (collection1.Count == 0 && collection2.Count == 0)
            return op == FilterOperator.Equals;

        if (collection1.Count == 0 || collection2.Count == 0)
            return op == FilterOperator.NotEquals;

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

        // // Fall back to string comparison
        // return CompareStrings(value1.ToString(), value2.ToString(), op);
    }

    // private static bool IsNumeric(object value)
    // {
    //     return value is sbyte || value is byte || value is short || value is ushort ||
    //            value is int || value is uint || value is long || value is ulong ||
    //            value is float || value is double || value is decimal;
    // }

    private static bool CompareStrings(PropertyValueCollection collection1, PropertyValueCollection collection2, FilterOperator op)
    {
        foreach (var value1 in collection1)
        {
            var s1 = (value1.Value as string)!;
            
            foreach (var value2 in collection2)
            {
                var s2 = (value2.Value as string)!;

                if (op switch
                    {
                        FilterOperator.Equals => string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase),
                        FilterOperator.NotEquals => !string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase),
                        FilterOperator.GreaterThan => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) > 0,
                        FilterOperator.LessThan => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) < 0,
                        FilterOperator.GreaterThanOrEqual => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) >= 0,
                        FilterOperator.LessThanOrEqual => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) <= 0,
                        FilterOperator.RegexMatch => Regex.IsMatch(s1, s2),
                        _ => throw new ArgumentException($"Unsupported string operator: {op}")
                    })
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool CompareDateTimes(PropertyValueCollection collection1, PropertyValueCollection collection2, FilterOperator op)
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
                        FilterOperator.Equals => d1 == d2,
                        FilterOperator.NotEquals => d1 != d2,
                        FilterOperator.GreaterThan => d1 > d2,
                        FilterOperator.LessThan => d1 < d2,
                        FilterOperator.GreaterThanOrEqual => d1 >= d2,
                        FilterOperator.LessThanOrEqual => d1 <= d2,
                        _ => throw new ArgumentException($"Unsupported datetime operator: {op}")
                    })
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool CompareNumbers(PropertyValueCollection collection1, PropertyValueCollection collection2, FilterOperator op)
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
                        FilterOperator.Equals => Math.Abs(n1 - n2) < epsilon, 
                        FilterOperator.NotEquals => Math.Abs(n1 - n2) >= epsilon,
                        FilterOperator.GreaterThan => n1 > n2,
                        FilterOperator.LessThan => n1 < n2,
                        FilterOperator.GreaterThanOrEqual => n1 >= n2,
                        FilterOperator.LessThanOrEqual => n1 <= n2,
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