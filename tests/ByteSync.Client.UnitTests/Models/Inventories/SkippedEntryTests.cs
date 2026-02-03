using ByteSync.Business.Inventories;
using ByteSync.Models.Inventories;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Models.Inventories;

[TestFixture]
public class SkippedEntryTests
{
    [Test]
    public void Constructor_ShouldSetDefaults()
    {
        // Arrange
        var before = DateTime.UtcNow;
        
        // Act
        var entry = new SkippedEntry();
        var after = DateTime.UtcNow;
        
        // Assert
        entry.FullPath.Should().BeEmpty();
        entry.RelativePath.Should().BeEmpty();
        entry.Name.Should().BeEmpty();
        entry.Reason.Should().Be(SkipReason.Unknown);
        entry.DetectedKind.Should().BeNull();
        entry.SkippedAt.Should().BeOnOrAfter(before);
        entry.SkippedAt.Should().BeOnOrBefore(after);
    }

    [Test]
    public void InitProperties_ShouldSetValues()
    {
        // Arrange
        var skippedAt = new DateTime(2025, 12, 1, 10, 30, 0, DateTimeKind.Utc);
        
        // Act
        var entry = new SkippedEntry
        {
            FullPath = "/data/test.txt",
            RelativePath = "/test.txt",
            Name = "test.txt",
            Reason = SkipReason.Hidden,
            DetectedKind = FileSystemEntryKind.RegularFile,
            SkippedAt = skippedAt
        };
        
        // Assert
        entry.FullPath.Should().Be("/data/test.txt");
        entry.RelativePath.Should().Be("/test.txt");
        entry.Name.Should().Be("test.txt");
        entry.Reason.Should().Be(SkipReason.Hidden);
        entry.DetectedKind.Should().Be(FileSystemEntryKind.RegularFile);
        entry.SkippedAt.Should().Be(skippedAt);
    }
}
