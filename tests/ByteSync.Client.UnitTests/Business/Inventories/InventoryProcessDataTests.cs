using System;
using System.Collections.Generic;
using ByteSync.Business.Inventories;
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
}
