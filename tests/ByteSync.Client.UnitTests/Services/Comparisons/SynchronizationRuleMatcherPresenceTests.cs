using System.Reflection;
using ByteSync.Business.Comparisons;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class SynchronizationRuleMatcherPresenceTests
{
    [Test]
    public void ExistsOn_File_ReturnsTrue_WhenContentIdentityHasInaccessibleDescription()
    {
        var matcher = new SynchronizationRuleMatcher(new Mock<IAtomicActionConsistencyChecker>().Object,
            new Mock<IAtomicActionRepository>().Object);

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
            // Mark as present but inaccessible
            // (IsAccessible defaults to true on base, override on the concrete instance)
            // The property exists on FileSystemDescription base
            // We set it to false explicitly
            // ReSharper disable once RedundantAssignment
            FingerprintMode = null
        };
        fd.IsAccessible = false;
        ci.Add(fd);

        comparisonItem.AddContentIdentity(ci);

        // DataPart that points to the same inventory part
        var dataPart = new DataPart("A", part);

        // Invoke private ExistsOn via reflection
        var existsOn = typeof(SynchronizationRuleMatcher)
            .GetMethod("ExistsOn", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)existsOn.Invoke(matcher, new object[] { dataPart, comparisonItem })!;

        result.Should().BeTrue();
    }
}

