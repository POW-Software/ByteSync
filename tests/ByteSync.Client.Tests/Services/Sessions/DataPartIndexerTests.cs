using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Inventories;
using ByteSync.Services.Sessions;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Sessions;

[TestFixture]
public class DataPartIndexerTests
{
    private DataPartIndexer _dataPartIndexer;

    [SetUp]
    public void SetUp()
    {
        _dataPartIndexer = new DataPartIndexer();
    }

    [Test]
    public void BuildMap_ShouldMapSingleInventoryCorrectly()
    {
        // Arrange
        var inventory = new Inventory
        {
            InventoryParts = [new InventoryPart()]
        };

        // Act
        _dataPartIndexer.BuildMap([inventory]);

        // Assert
        var dataParts = _dataPartIndexer.GetAllDataParts();
        dataParts.Should().HaveCount(1);
        dataParts[0].Name.Should().Be("A");
        dataParts[0].Inventory.Should().Be(inventory);
    }

    [Test]
    public void BuildMap_ShouldMapMultipleInventoryPartsCorrectly()
    {
        // Arrange
        var inventory = new Inventory
        {
            InventoryParts = [new InventoryPart(), new InventoryPart()]
        };

        // Act
        _dataPartIndexer.BuildMap([inventory]);

        // Assert
        var dataParts = _dataPartIndexer.GetAllDataParts();
        dataParts.Should().HaveCount(2);
        dataParts[0].Name.Should().Be("A1");
        dataParts[1].Name.Should().Be("A2");
    }

    [Test]
    public void GetDataPart_ShouldReturnCorrectDataPart_WhenNameExists()
    {
        // Arrange
        var inventory = new Inventory
        {
            InventoryParts = [new InventoryPart()]
        };
        _dataPartIndexer.BuildMap([inventory]);

        // Act
        var dataPart = _dataPartIndexer.GetDataPart("A");

        // Assert
        dataPart.Should().NotBeNull();
        dataPart.Name.Should().Be("A");
    }

    [Test]
    public void GetDataPart_ShouldReturnNull_WhenNameDoesNotExist()
    {
        // Act
        var dataPart = _dataPartIndexer.GetDataPart("NonExistent");

        // Assert
        dataPart.Should().BeNull();
    }

    [Test]
    public void Remap_ShouldUpdateActionsAndConditionsCorrectly()
    {
        // Arrange
        var inventory = new Inventory
        {
            InventoryParts = [new InventoryPart()]
        };
        _dataPartIndexer.BuildMap([inventory]);

        var sourcePart = _dataPartIndexer.GetDataPart("A");
        var synchronizationRule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
        synchronizationRule.AddAction(new AtomicAction { Source = sourcePart, Destination = sourcePart });
        synchronizationRule.Conditions.Add(new AtomicCondition { Source = sourcePart!, Destination = sourcePart });

        // Act
        _dataPartIndexer.Remap(new List<SynchronizationRule> { synchronizationRule });

        // Assert
        synchronizationRule.Actions[0].Source.Should().Be(sourcePart);
        synchronizationRule.Conditions[0].Source.Should().Be(sourcePart);
    }
}
