using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons;
using ByteSync.Tests.TestUtilities.Helpers;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using ByteSync.Business;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;
using ByteSync.TestsCommon;

namespace ByteSync.Tests.Services.Comparisons;

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
            Letter = "A",
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
            Letter = "A",
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
}
