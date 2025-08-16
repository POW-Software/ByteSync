using System.Globalization;
using System.Text.RegularExpressions;

namespace ByteSync.Business.Filtering.Extensions;

public static class SizeUnitExtensions
{
    public static long ToBytes(this string sizeWithUnit)
    {
        var safeRegex = new Regex(@"^(\d+(?:\.\d+)?)\s*([a-zA-Z]+)$", RegexOptions.None, TimeSpan.FromMilliseconds(500));
        var match = safeRegex.Match(sizeWithUnit);
        if (!match.Success)
            return long.Parse(sizeWithUnit); // Assume bytes if no unit specified

        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Value.ToLowerInvariant();

        return unit switch
        {
            "b" or "bytes" => (long)value,
            "kb" or "kilobytes" => (long)(value * 1024),
            "mb" or "megabytes" => (long)(value * 1024 * 1024),
            "gb" or "gigabytes" => (long)(value * 1024 * 1024 * 1024),
            "tb" or "terabytes" => (long)(value * 1024 * 1024 * 1024 * 1024),
            _ => throw new ArgumentException($"Unknown size unit: {unit}")
        };
    }
}