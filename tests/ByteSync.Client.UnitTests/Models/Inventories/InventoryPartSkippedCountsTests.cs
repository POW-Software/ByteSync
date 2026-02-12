using ByteSync.Models.Inventories;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Models.Inventories;

[TestFixture]
public class InventoryPartSkippedCountsTests
{
    [Test]
    public void RecordSkippedEntry_ShouldUpdateTotalAndReasonCounts()
    {
        // Arrange
        var part = new InventoryPart();
        
        // Act
        part.RecordSkippedEntry(SkipReason.Hidden);
        part.RecordSkippedEntry(SkipReason.Hidden);
        part.RecordSkippedEntry(SkipReason.NoiseEntry);
        
        // Assert
        part.SkippedCount.Should().Be(3);
        part.GetSkippedCountByReason(SkipReason.Hidden).Should().Be(2);
        part.GetSkippedCountByReason(SkipReason.NoiseEntry).Should().Be(1);
        part.GetSkippedCountByReason(SkipReason.Offline).Should().Be(0);
    }

    [Test]
    public void SkippedCountsByReason_WhenSetToNull_ShouldFallbackToEmptyDictionary()
    {
        // Arrange
        var part = new InventoryPart();
        part.SkippedCountsByReason = null!;
        
        // Act
        var skippedCount = part.SkippedCount;
        var hiddenCount = part.GetSkippedCountByReason(SkipReason.Hidden);
        
        // Assert
        skippedCount.Should().Be(0);
        hiddenCount.Should().Be(0);
    }
}
