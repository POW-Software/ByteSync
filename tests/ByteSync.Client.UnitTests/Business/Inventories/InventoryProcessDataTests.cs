using System;
using System.Collections.Generic;
using ByteSync.Business.Inventories;
using ByteSync.Models.Inventories;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Business.Inventories;

[TestFixture]
public class InventoryProcessDataTests
{
    [Test]
    public void SetError_ShouldUpdateLastException_AndRaiseEvent()
    {
        // Arrange
        var data = new InventoryProcessData();
        var values = new List<bool>();
        data.ErrorEvent.Subscribe(values.Add);
        var exception = new InvalidOperationException("boom");
        
        // Act
        data.SetError(exception);
        
        // Assert
        data.LastException.Should().Be(exception);
        values.Should().Contain(true);
    }
    
    [Test]
    public void RecordSkippedEntry_ShouldUpdateGlobalAndReasonCounters()
    {
        // Arrange
        var data = new InventoryProcessData();
        
        // Act
        data.RecordSkippedEntry(new SkippedEntry { Reason = SkipReason.Hidden });
        data.RecordSkippedEntry(new SkippedEntry { Reason = SkipReason.Hidden });
        data.RecordSkippedEntry(new SkippedEntry { Reason = SkipReason.NoiseEntry });
        
        // Assert
        data.SkippedCount.Should().Be(3);
        data.GetSkippedCountByReason(SkipReason.Hidden).Should().Be(2);
        data.GetSkippedCountByReason(SkipReason.NoiseEntry).Should().Be(1);
        data.GetSkippedCountByReason(SkipReason.Offline).Should().Be(0);
    }
    
    [Test]
    public void Reset_ShouldClearSkippedEntriesAndCounters()
    {
        // Arrange
        var data = new InventoryProcessData();
        data.RecordSkippedEntry(new SkippedEntry { Reason = SkipReason.Hidden });
        data.RecordSkippedEntry(new SkippedEntry { Reason = SkipReason.NoiseEntry });
        
        // Act
        data.Reset();
        
        // Assert
        data.SkippedEntries.Should().BeEmpty();
        data.SkippedCount.Should().Be(0);
        data.GetSkippedCountByReason(SkipReason.Hidden).Should().Be(0);
        data.GetSkippedCountByReason(SkipReason.NoiseEntry).Should().Be(0);
    }
}
