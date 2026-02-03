using ByteSync.Business.Inventories;

namespace ByteSync.Models.Inventories;

public class SkippedEntry
{
    public string FullPath { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public SkipReason Reason { get; init; }

    public FileSystemEntryKind? DetectedKind { get; init; }

    public DateTime SkippedAt { get; init; } = DateTime.UtcNow;
}
