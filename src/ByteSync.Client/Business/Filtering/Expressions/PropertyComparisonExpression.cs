using System.Text.RegularExpressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Expressions;

public class PropertyComparisonExpression : FilterExpression
{
    private readonly string _sourceDataSource;
    private readonly string _property;
    private readonly string _operator;
    private readonly string _targetDataSource;
    private readonly string _targetValue;

    public PropertyComparisonExpression(string sourceDataSource, string property, string @operator, string targetDataSource, string targetValue = null)
    {
        _sourceDataSource = sourceDataSource;
        _property = property;
        _operator = @operator;
        _targetDataSource = targetDataSource;
        _targetValue = targetValue;
    }

    public override bool Evaluate(ComparisonItem item)
    {
        // This is a simplified implementation that needs to be expanded
        // based on the actual structure of ComparisonItem

        switch (_property.ToLowerInvariant())
        {
            case "content":
                return CompareContent(item);
            case "contentanddate":
                return CompareContentAndDate(item);
            case "size":
                return CompareSize(item);
            case "date":
                return CompareDate(item);
            case "ext":
                return CompareExtension(item);
            case "name":
                return CompareName(item);
            case "path":
                return ComparePath(item);
            default:
                return false;
        }
    }

    private bool CompareContent(ComparisonItem item)
    {
        // Implementation depends on actual content comparison logic
        // This is a placeholder
        return false;
    }

    private bool CompareContentAndDate(ComparisonItem item)
    {
        // Implementation depends on actual content and date comparison logic
        // This is a placeholder
        return false;
    }

    private bool CompareSize(ComparisonItem item)
    {
        // Implementation for size comparison
        // This is a placeholder
        return false;
    }

    private bool CompareDate(ComparisonItem item)
    {
        // Implementation for date comparison
        // This is a placeholder
        return false;
    }

    private bool CompareExtension(ComparisonItem item)
    {
        if (_operator == "==" || _operator == "=")
        {
            var extension = System.IO.Path.GetExtension(item.PathIdentity.FileName);
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            return extension.Equals(_targetValue, StringComparison.OrdinalIgnoreCase);
        }
        else if (_operator == "=~")
        {
            var extension = System.IO.Path.GetExtension(item.PathIdentity.FileName);
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            return Regex.IsMatch(extension, _targetValue);
        }

        return false;
    }

    private bool CompareName(ComparisonItem item)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(item.PathIdentity.FileName);

        if (_operator == "==" || _operator == "=")
        {
            return fileName.Equals(_targetValue, StringComparison.OrdinalIgnoreCase);
        }
        else if (_operator == "=~")
        {
            return Regex.IsMatch(fileName, _targetValue);
        }

        return false;
    }

    private bool ComparePath(ComparisonItem item)
    {
        if (_operator == "==" || _operator == "=")
        {
            return item.PathIdentity.FileName.Equals(_targetValue, StringComparison.OrdinalIgnoreCase);
        }
        else if (_operator == "=~")
        {
            return Regex.IsMatch(item.PathIdentity.FileName, _targetValue);
        }

        return false;
    }
}