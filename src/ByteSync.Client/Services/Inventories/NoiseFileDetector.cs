using ByteSync.Common.Business.Misc;

namespace ByteSync.Services.Inventories;

public static class NoiseFileDetector
{
    private static readonly string[] KnownNoiseFileNames =
    [
        "desktop.ini",
        "thumbs.db",
        "ehthumbs.db",
        "ehthumbs_vista.db",
        ".desktop.ini",
        ".thumbs.db",
        ".DS_Store",
        ".AppleDouble",
        ".LSOverride",
        ".Spotlight-V100",
        ".Trashes",
        ".fseventsd",
        ".TemporaryItems",
        ".VolumeIcon.icns",
        ".directory"
    ];

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
}
