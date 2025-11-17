using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class SynchronizationRuleMatcherLocalizeTests
{
    private ContentIdentityExtractor _extractor = null!;
    
    [SetUp]
    public void SetUp()
    {
        _extractor = new ContentIdentityExtractor();
    }
    
    [Test]
    public void LocalizeContentIdentity_WithInventory_ReturnsMatchingContentIdentity()
    {
        var inventoryA = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var inventoryB = new Inventory { InventoryId = "INV_B", Code = "B", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventoryA, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventoryB, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 100 });
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 200 });
        contentIdentityA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentityB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var dataPart = new DataPart("A", inventoryA);
        
        var result = _extractor.LocalizeContentIdentity(dataPart, comparisonItem);
        
        result.Should().Be(contentIdentityA);
    }
    
    [Test]
    public void LocalizeContentIdentity_WithInventoryPart_ReturnsMatchingContentIdentity()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 100 });
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 200 });
        contentIdentityA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentityB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var dataPart = new DataPart("A", partA);
        
        var result = _extractor.LocalizeContentIdentity(dataPart, comparisonItem);
        
        result.Should().Be(contentIdentityA);
    }
    
    [Test]
    public void LocalizeContentIdentity_WithNoMatch_ReturnsNull()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        var otherPart = new InventoryPart(inventory, "c:/other", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        contentIdentity.Add(new FileDescription { InventoryPart = otherPart, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var dataPart = new DataPart("A", part);
        
        var result = _extractor.LocalizeContentIdentity(dataPart, comparisonItem);
        
        result.Should().BeNull();
    }
    
    [Test]
    public void ExtractContentIdentity_WithNullDataPart_ReturnsNull()
    {
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var result = _extractor.ExtractContentIdentity(null, comparisonItem);
        
        result.Should().BeNull();
    }
    
    [Test]
    public void ExtractSize_WithContentIdentity_ReturnsSize()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 1024 });
        contentIdentity.Add(new FileDescription { InventoryPart = part, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var dataPart = new DataPart("A", part);
        
        var result = _extractor.ExtractSize(dataPart, comparisonItem);
        
        result.Should().Be(1024);
    }
    
    [Test]
    public void ExtractSize_WithNoContentIdentity_ReturnsNull()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var dataPart = new DataPart("A", part);
        
        var result = _extractor.ExtractSize(dataPart, comparisonItem);
        
        result.Should().BeNull();
    }
    
    [Test]
    public void ExtractDate_WithContentIdentity_ReturnsLastWriteTime()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        
        var date = DateTime.UtcNow;
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescription = new FileDescription
        {
            InventoryPart = part,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = date
        };
        contentIdentity.Add(fileDescription);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var dataPart = new DataPart("A", part);
        
        var result = _extractor.ExtractDate(dataPart, comparisonItem);
        
        result.Should().Be(date);
    }
    
    [Test]
    public void ExtractDate_WithNoContentIdentity_ReturnsNull()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var dataPart = new DataPart("A", part);
        
        var result = _extractor.ExtractDate(dataPart, comparisonItem);
        
        result.Should().BeNull();
    }
    
    [Test]
    public void ExtractDate_WithContentIdentityButNoMatchingInventoryPart_ReturnsNull()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var date = DateTime.UtcNow;
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescription = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = date
        };
        contentIdentity.Add(fileDescription);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var dataPart = new DataPart("A", partB);
        
        var result = _extractor.ExtractDate(dataPart, comparisonItem);
        
        result.Should().BeNull();
    }
}