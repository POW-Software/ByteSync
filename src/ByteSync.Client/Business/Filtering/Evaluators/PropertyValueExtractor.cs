using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Business.Filtering.Values;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;

namespace ByteSync.Business.Filtering.Evaluators;

public class PropertyValueExtractor : IPropertyValueExtractor
{
    public PropertyValueCollection GetPropertyValue(ComparisonItem item, DataPart? dataPart, string property)
    {
        if (dataPart == null)
        {
            return GetGeneralPropertyValue(item, property);
        }

        // Find content identities for the specific data source
        var contentIdentities = item.GetContentIdentities(dataPart.GetApplicableInventoryPart());

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
    }

    private PropertyValueCollection ExtractContent(List<ContentIdentity> contentIdentities)
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
    
    private PropertyValueCollection ExtractContentAndDate(List<ContentIdentity> contentIdentities, DataPart dataPart)
    {
        var contents = new HashSet<string>();

        foreach (var contentIdentity in contentIdentities)
        {
            var signatureHash = contentIdentity.Core!.SignatureHash;
            var lastWriteTime = contentIdentity.GetLastWriteTimeUtc(dataPart.GetApplicableInventoryPart());
            
            contents.Add($"{signatureHash}_{lastWriteTime?.Ticks}");
        }

        var result = new PropertyValueCollection();
        foreach (var content in contents)
        {
            result.Add(new PropertyValue(content));
        }

        return result;
    }
    
    private PropertyValueCollection ExtractSize(List<ContentIdentity> contentIdentities, DataPart dataPart)
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
        }

        var result = new PropertyValueCollection();
        foreach (var content in contents)
        {
            result.Add(new PropertyValue(content));
        }

        return result;
    }
    
    private PropertyValueCollection ExtractLastWriteTime(List<ContentIdentity> contentIdentities, DataPart dataPart)
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
    private PropertyValueCollection GetGeneralPropertyValue(ComparisonItem item, string property)
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
}