using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Inventories;
using ByteSync.Services.Sessions;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Sessions;

[TestFixture]
public class DataPartIndexerTests
{
    private DataPartIndexer _dataPartIndexer = null!;
    
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
            Code = "A",
            InventoryParts = [new InventoryPart { Code = "A" }] // InventoryPart gets same code for single part
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
            Code = "A",
            InventoryParts =
            [
                new InventoryPart { Code = "A1" }, // Each part gets its own code
                new InventoryPart { Code = "A2" }
            ]
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
            Code = "A",
            InventoryParts = [new InventoryPart { Code = "A" }] // Single part gets same code as inventory
        };
        _dataPartIndexer.BuildMap([inventory]);
        
        // Act
        var dataPart = _dataPartIndexer.GetDataPart("A");
        
        // Assert
        dataPart.Should().NotBeNull();
        dataPart.Name.Should().Be("A");
    }
    
    [Test]
    public void GetDataPart_ShouldReturnCorrectDataPart_WhenSinglePartInventoryAndInventoryPartName()
    {
        // Arrange
        var inventory = new Inventory
        {
            Code = "A",
            InventoryParts = [new InventoryPart { Code = "A" }] // Single part gets same code as inventory
        };
        _dataPartIndexer.BuildMap([inventory]);
        
        // Act
        var dataPart = _dataPartIndexer.GetDataPart("A1");
        
        // Assert
        dataPart.Should().NotBeNull();
        dataPart.Name.Should().Be("A"); // Should return "A" part for compatibility
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
            Code = "A",
            InventoryParts = [new InventoryPart { Code = "A" }] // Single part gets same code as inventory
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
    
    [Test]
    public void BuildMap_ShouldUseInventoryCodesWhenAllInventoriesHaveSinglePart()
    {
        // Arrange
        var inventoryA = new Inventory
        {
            Code = "A",
            InventoryParts = [new InventoryPart { Code = "A1" }]
        };
        var inventoryB = new Inventory
        {
            Code = "B",
            InventoryParts = [new InventoryPart { Code = "B1" }]
        };
        
        // Act
        _dataPartIndexer.BuildMap([inventoryA, inventoryB]);
        
        // Assert
        var dataParts = _dataPartIndexer.GetAllDataParts();
        dataParts.Should().HaveCount(2);
        
        var dataPartNames = dataParts.Select(dp => dp.Name).OrderBy(name => name).ToList();
        dataPartNames.Should().BeEquivalentTo(["A", "B"]);
        
        var dataPartA = _dataPartIndexer.GetDataPart("A");
        var dataPartB = _dataPartIndexer.GetDataPart("B");
        
        dataPartA.Should().NotBeNull();
        dataPartA.Inventory.Should().Be(inventoryA);
        dataPartB.Should().NotBeNull();
        dataPartB.Inventory.Should().Be(inventoryB);
    }
    
    [Test]
    public void BuildMap_ShouldUseInventoryPartCodesWhenSomeInventoriesHaveMultipleParts()
    {
        // Arrange
        var inventoryA = new Inventory
        {
            Code = "A",
            InventoryParts = [new InventoryPart { Code = "A1" }]
        };
        var inventoryB = new Inventory
        {
            Code = "B",
            InventoryParts =
            [
                new InventoryPart { Code = "B1" },
                new InventoryPart { Code = "B2" }
            ]
        };
        
        // Act
        _dataPartIndexer.BuildMap([inventoryA, inventoryB]);
        
        // Assert
        var dataParts = _dataPartIndexer.GetAllDataParts();
        dataParts.Should().HaveCount(3);
        
        var dataPartNames = dataParts.Select(dp => dp.Name).OrderBy(name => name).ToList();
        dataPartNames.Should().BeEquivalentTo(["A1", "B1", "B2"]);
    }
    
    [Test]
    public void BuildMap_ShouldHandleMixedSinglePartInventoriesWithDifferentNamingConventions()
    {
        // Arrange
        var inventoryA = new Inventory
        {
            Code = "A",
            InventoryParts = [new InventoryPart { Code = "A" }]
        };
        var inventoryB = new Inventory
        {
            Code = "B",
            InventoryParts = [new InventoryPart { Code = "B" }]
        };
        var inventoryC = new Inventory
        {
            Code = "C",
            InventoryParts = [new InventoryPart { Code = "C" }]
        };
        
        // Act
        _dataPartIndexer.BuildMap([inventoryA, inventoryB, inventoryC]);
        
        // Assert
        var dataParts = _dataPartIndexer.GetAllDataParts();
        dataParts.Should().HaveCount(3);
        
        var dataPartNames = dataParts.Select(dp => dp.Name).OrderBy(name => name).ToList();
        dataPartNames.Should().BeEquivalentTo(["A", "B", "C"]);
        
        foreach (var inventory in new[] { inventoryA, inventoryB, inventoryC })
        {
            var dataPart = _dataPartIndexer.GetDataPart(inventory.Code);
            dataPart.Should().NotBeNull();
            dataPart.Inventory.Should().Be(inventory);
        }
    }
}