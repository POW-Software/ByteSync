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
public class SynchronizationRuleMatcherPresenceTests
{
    [Test]
    public void ExistsOn_File_ReturnsTrue_WhenContentIdentityHasInaccessibleDescription()
    {
        var extractor = new ContentIdentityExtractor();
        
        // Build a comparison item for a file
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        // Inventory and part to associate
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        
        // Content identity contains a file description marked as inaccessible
        var ci = new ContentIdentity(null);
        var fd = new FileDescription
        {
            InventoryPart = part,
            RelativePath = "/file.txt",
            FingerprintMode = null
        };
        fd.IsAccessible = false;
        ci.Add(fd);
        
        comparisonItem.AddContentIdentity(ci);
        
        // DataPart that points to the same inventory part
        var dataPart = new DataPart("A", part);
        
        var result = extractor.ExistsOn(dataPart, comparisonItem);
        
        result.Should().BeTrue();
    }
}