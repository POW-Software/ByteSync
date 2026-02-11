using System.Reflection;
using System.Text.Json;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Services.Inventories;

public static class NoiseFileDetector
{
    private const string NoiseFileResourceSuffix = ".Services.Inventories.noise-files.json";
    private static readonly string[] KnownNoiseFileNames = LoadNoiseFileNames();

    private static readonly HashSet<string> CaseSensitiveNoiseFileNames = new(KnownNoiseFileNames, StringComparer.Ordinal);
    private static readonly HashSet<string> CaseInsensitiveNoiseFileNames = new(KnownNoiseFileNames, StringComparer.OrdinalIgnoreCase);

    public static bool IsNoiseFileName(string? fileName, OSPlatforms os)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return os == OSPlatforms.Linux
            ? CaseSensitiveNoiseFileNames.Contains(fileName)
            : CaseInsensitiveNoiseFileNames.Contains(fileName);
    }

    private static string[] LoadNoiseFileNames()
    {
        var assembly = typeof(NoiseFileDetector).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(rn => rn.EndsWith(NoiseFileResourceSuffix, StringComparison.Ordinal));

        if (resourceName == null)
        {
            throw new InvalidOperationException($"Embedded resource not found: '*{NoiseFileResourceSuffix}'");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Unable to open embedded resource stream: '{resourceName}'");
        }

        var parsed = JsonSerializer.Deserialize<string[]>(stream);
        if (parsed == null)
        {
            throw new InvalidOperationException($"Unable to deserialize embedded resource: '{resourceName}'");
        }

        return parsed
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
