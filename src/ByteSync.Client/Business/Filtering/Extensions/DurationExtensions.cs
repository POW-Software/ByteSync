using System.Globalization;
using System.Text.RegularExpressions;

namespace ByteSync.Business.Filtering.Extensions;

public static class DurationExtensions
{
    public static TimeSpan ToTimeSpan(this string durationWithUnit)
    {
        var match = Regex.Match(durationWithUnit, @"^(\d+(?:\.\d+)?)\s*([a-zA-Z]+)$");
        if (!match.Success)
            return TimeSpan.FromSeconds(double.Parse(durationWithUnit)); // Assume seconds if no unit

        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Value.ToLowerInvariant();

        return unit switch
        {
            "s" or "sec" or "second" or "seconds" => TimeSpan.FromSeconds(value),
            "m" or "min" or "minute" or "minutes" => TimeSpan.FromMinutes(value),
            "h" or "hour" or "hours" => TimeSpan.FromHours(value),
            "d" or "day" or "days" => TimeSpan.FromDays(value),
            _ => throw new ArgumentException($"Unknown duration unit: {unit}")
        };
    }
}