using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using ByteSync.TestsCommon;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class InitialStatusBuilderTests : AbstractTester
{
    [Test]
    public void Test_BuildStatus_ForFile_With_FileDescription()
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var contentIdentityCore = new ContentIdentityCore
        {
            SignatureHash = "hash1",
            Size = 100
        };
        var contentIdentity = new ContentIdentity(contentIdentityCore);
        
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var id = $"IID_{Guid.NewGuid()}";
        var inventory = new Inventory
        {
            Code = "A",
            InventoryId = id
        };
        var inventoryPart = new InventoryPart(inventory, "rootpath", FileSystemTypes.File);
        inventory.Add(inventoryPart);
        
        var fileDescription = new FileDescription
        {
            InventoryPart = inventoryPart,
            LastWriteTimeUtc = DateTime.UtcNow,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "hash1"
        };
        
        contentIdentity.Add(fileDescription);
        
        var initialStatusBuilder = new InitialStatusBuilder();
        
        // Act
        initialStatusBuilder.BuildStatus(comparisonItem, [inventory]);
        
        // Assert
        comparisonItem.ContentRepartition.MissingInventories.Should().BeEmpty();
        comparisonItem.ContentRepartition.MissingInventoryParts.Should().BeEmpty();
    }
    
    [Test]
    public void Test_BuildStatus_ForFile_Without_FileDescription()
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var contentIdentityCore = new ContentIdentityCore
        {
            SignatureHash = "hash1",
            Size = 100
        };
        var contentIdentity = new ContentIdentity(contentIdentityCore);
        
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var id = $"IID_{Guid.NewGuid()}";
        var inventory = new Inventory
        {
            Code = "A",
            InventoryId = id
        };
        var inventoryPart = new InventoryPart(inventory, "rootpath", FileSystemTypes.File);
        inventory.Add(inventoryPart);
        
        var initialStatusBuilder = new InitialStatusBuilder();
        
        // Act
        initialStatusBuilder.BuildStatus(comparisonItem, [inventory]);
        
        // Assert
        comparisonItem.ContentRepartition.MissingInventories.Should().NotBeEmpty();
        comparisonItem.ContentRepartition.MissingInventoryParts.Should().NotBeEmpty();
    }
    
    [Test]
    public void Test_BuildStatus_ForDirectory_With_DirectoryDescription()
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.Directory, "/dir1", "dir1", "dir1");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var contentIdentityCore = new ContentIdentityCore
        {
            SignatureHash = "dirhash1",
            Size = 0
        };
        var contentIdentity = new ContentIdentity(contentIdentityCore);
        
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var id = $"IID_{Guid.NewGuid()}";
        var inventory = new Inventory
        {
            Code = "A",
            InventoryId = id
        };
        var inventoryPart = new InventoryPart(inventory, "rootpath", FileSystemTypes.Directory);
        inventory.Add(inventoryPart);
        
        var directoryDescription = new DirectoryDescription
        {
            InventoryPart = inventoryPart
        };
        
        contentIdentity.Add(directoryDescription);
        
        var initialStatusBuilder = new InitialStatusBuilder();
        
        // Act
        initialStatusBuilder.BuildStatus(comparisonItem, [inventory]);
        
        // Assert
        comparisonItem.ContentRepartition.MissingInventories.Should().BeEmpty();
        comparisonItem.ContentRepartition.MissingInventoryParts.Should().BeEmpty();
    }
    
    [Test]
    public void Test_BuildStatus_ForDirectory_Without_DirectoryDescription()
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.Directory, "/dir1", "dir1", "dir1");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var contentIdentityCore = new ContentIdentityCore
        {
            SignatureHash = "dirhash1",
            Size = 0
        };
        var contentIdentity = new ContentIdentity(contentIdentityCore);
        
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var id = $"IID_{Guid.NewGuid()}";
        var inventory = new Inventory
        {
            Code = "A",
            InventoryId = id
        };
        var inventoryPart = new InventoryPart(inventory, "rootpath", FileSystemTypes.Directory);
        inventory.Add(inventoryPart);
        
        var initialStatusBuilder = new InitialStatusBuilder();
        
        // Act
        initialStatusBuilder.BuildStatus(comparisonItem, [inventory]);
        
        // Assert
        comparisonItem.ContentRepartition.MissingInventories.Should().NotBeEmpty();
        comparisonItem.ContentRepartition.MissingInventoryParts.Should().NotBeEmpty();
    }
    
    [Test]
    public void Test_BuildStatus_ForFile_With_Multiple_ContentIdentities()
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        // First content identity
        var contentIdentityCore1 = new ContentIdentityCore
        {
            SignatureHash = "hash1",
            Size = 100
        };
        var contentIdentity1 = new ContentIdentity(contentIdentityCore1);
        
        // Second content identity (different hash and size)
        var contentIdentityCore2 = new ContentIdentityCore
        {
            SignatureHash = "hash2",
            Size = 120
        };
        var contentIdentity2 = new ContentIdentity(contentIdentityCore2);
        
        comparisonItem.AddContentIdentity(contentIdentity1);
        comparisonItem.AddContentIdentity(contentIdentity2);
        
        var id = $"IID_{Guid.NewGuid()}";
        var inventory = new Inventory
        {
            Code = "A",
            InventoryId = id
        };
        
        // Create inventory part for first content identity
        var inventoryPart1 = new InventoryPart(inventory, "rootpath", FileSystemTypes.File);
        inventory.Add(inventoryPart1);
        
        var fileDescription1 = new FileDescription
        {
            InventoryPart = inventoryPart1,
            LastWriteTimeUtc = DateTime.UtcNow,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "hash1"
        };
        
        contentIdentity1.Add(fileDescription1);
        
        // Create inventory part for second content identity
        var inventoryPart2 = new InventoryPart(inventory, "rootpath2", FileSystemTypes.File);
        inventory.Add(inventoryPart2);
        
        var fileDescription2 = new FileDescription
        {
            InventoryPart = inventoryPart2,
            LastWriteTimeUtc = DateTime.UtcNow,
            Size = 120,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "hash2"
        };
        
        contentIdentity2.Add(fileDescription2);
        
        var initialStatusBuilder = new InitialStatusBuilder();
        
        // Act
        initialStatusBuilder.BuildStatus(comparisonItem, [inventory]);
        
        // Assert
        comparisonItem.ContentRepartition.MissingInventories.Should().BeEmpty();
        comparisonItem.ContentRepartition.MissingInventoryParts.Should().BeEmpty();
        comparisonItem.ContentRepartition.FingerPrintGroups.Should().HaveCount(2);
        comparisonItem.ContentRepartition.FingerPrintGroups[contentIdentityCore1].Should().Contain(inventoryPart1);
        comparisonItem.ContentRepartition.FingerPrintGroups[contentIdentityCore2].Should().Contain(inventoryPart2);
    }
    
    [Test]
    public void Test_BuildStatus_ForFile_With_Different_LastWriteTimes()
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var contentIdentityCore = new ContentIdentityCore
        {
            SignatureHash = "hash1",
            Size = 100
        };
        var contentIdentity = new ContentIdentity(contentIdentityCore);
        
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var id = $"IID_{Guid.NewGuid()}";
        var inventory = new Inventory
        {
            Code = "A",
            InventoryId = id
        };
        
        // First inventory part with one timestamp
        var inventoryPart1 = new InventoryPart(inventory, "rootpath1", FileSystemTypes.File);
        inventory.Add(inventoryPart1);
        
        var lastWriteTime1 = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var fileDescription1 = new FileDescription
        {
            InventoryPart = inventoryPart1,
            LastWriteTimeUtc = lastWriteTime1,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "hash1"
        };
        
        contentIdentity.Add(fileDescription1);
        
        // Second inventory part with different timestamp
        var inventoryPart2 = new InventoryPart(inventory, "rootpath2", FileSystemTypes.File);
        inventory.Add(inventoryPart2);
        
        var lastWriteTime2 = new DateTime(2023, 1, 2, 10, 0, 0, DateTimeKind.Utc);
        var fileDescription2 = new FileDescription
        {
            InventoryPart = inventoryPart2,
            LastWriteTimeUtc = lastWriteTime2,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "hash1"
        };
        
        contentIdentity.Add(fileDescription2);
        
        var initialStatusBuilder = new InitialStatusBuilder();
        
        // Act
        initialStatusBuilder.BuildStatus(comparisonItem, [inventory]);
        
        // Assert
        comparisonItem.ContentRepartition.MissingInventories.Should().BeEmpty();
        comparisonItem.ContentRepartition.MissingInventoryParts.Should().BeEmpty();
        comparisonItem.ContentRepartition.LastWriteTimeGroups.Should().HaveCount(2);
        comparisonItem.ContentRepartition.LastWriteTimeGroups[lastWriteTime1].Should().Contain(inventoryPart1);
        comparisonItem.ContentRepartition.LastWriteTimeGroups[lastWriteTime2].Should().Contain(inventoryPart2);
    }
    
    [Test]
    public void Test_BuildStatus_ForFile_With_Multiple_Inventories()
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var contentIdentityCore = new ContentIdentityCore
        {
            SignatureHash = "hash1",
            Size = 100
        };
        var contentIdentity = new ContentIdentity(contentIdentityCore);
        
        comparisonItem.AddContentIdentity(contentIdentity);
        
        // First inventory
        var id1 = $"IID_{Guid.NewGuid()}";
        var inventory1 = new Inventory
        {
            Code = "A",
            InventoryId = id1
        };
        var inventoryPart1 = new InventoryPart(inventory1, "rootpath1", FileSystemTypes.File);
        inventory1.Add(inventoryPart1);
        
        var fileDescription1 = new FileDescription
        {
            InventoryPart = inventoryPart1,
            LastWriteTimeUtc = DateTime.UtcNow,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "hash1"
        };
        
        contentIdentity.Add(fileDescription1);
        
        // Second inventory
        var id2 = $"IID_{Guid.NewGuid()}";
        var inventory2 = new Inventory
        {
            Code = "B",
            InventoryId = id2
        };
        var inventoryPart2 = new InventoryPart(inventory2, "rootpath2", FileSystemTypes.File);
        inventory2.Add(inventoryPart2);
        
        var fileDescription2 = new FileDescription
        {
            InventoryPart = inventoryPart2,
            LastWriteTimeUtc = DateTime.UtcNow,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "hash1"
        };
        
        contentIdentity.Add(fileDescription2);
        
        var initialStatusBuilder = new InitialStatusBuilder();
        
        // Act
        initialStatusBuilder.BuildStatus(comparisonItem, [inventory1, inventory2]);
        
        // Assert
        comparisonItem.ContentRepartition.MissingInventories.Should().BeEmpty();
        comparisonItem.ContentRepartition.MissingInventoryParts.Should().BeEmpty();
        comparisonItem.ContentRepartition.FingerPrintGroups.Should().HaveCount(1);
        comparisonItem.ContentRepartition.FingerPrintGroups[contentIdentityCore].Should().Contain(inventoryPart1);
        comparisonItem.ContentRepartition.FingerPrintGroups[contentIdentityCore].Should().Contain(inventoryPart2);
    }
}