using System.IO.Compression;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Controls.Json;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

[TestFixture]
public class InventoryLoaderIncompleteFlagTests
{
    private string _tempDirectory = null!;
    
    [SetUp]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }
    
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
    
    [Test]
    public void Load_ShouldMarkPartIncomplete_WhenInaccessibleDescriptionsExist()
    {
        var inventory = BuildInventoryWithInaccessibleDirectory();
        var zipPath = CreateInventoryZipFile(_tempDirectory, inventory);
        
        using var loader = new InventoryLoader(zipPath);
        var loadedInventory = loader.Inventory;
        
        loadedInventory.InventoryParts.Should().HaveCount(1);
        loadedInventory.InventoryParts[0].IsIncompleteDueToAccess.Should().BeTrue();
    }
    
    [Test]
    public void Load_ShouldKeepSkippedCountsByReason()
    {
        // Arrange
        var inventory = BuildInventoryWithInaccessibleDirectory();
        var part = inventory.InventoryParts[0];
        part.RecordSkippedEntry(SkipReason.Hidden);
        part.RecordSkippedEntry(SkipReason.Hidden);
        part.RecordSkippedEntry(SkipReason.NoiseEntry);
        var zipPath = CreateInventoryZipFile(_tempDirectory, inventory);
        
        // Act
        using var loader = new InventoryLoader(zipPath);
        var loadedPart = loader.Inventory.InventoryParts[0];
        
        // Assert
        loadedPart.SkippedCount.Should().Be(3);
        loadedPart.GetSkippedCountByReason(SkipReason.Hidden).Should().Be(2);
        loadedPart.GetSkippedCountByReason(SkipReason.NoiseEntry).Should().Be(1);
        loadedPart.GetSkippedCountByReason(SkipReason.Offline).Should().Be(0);
    }
    
    private static Inventory BuildInventoryWithInaccessibleDirectory()
    {
        var inventory = new Inventory
        {
            InventoryId = "INV_A",
            Code = "A",
            MachineName = "MachineA",
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = "CII_A",
                OSPlatform = OSPlatforms.Windows
            }
        };
        
        var part = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory)
        {
            Code = "A1",
            IsIncompleteDueToAccess = false
        };
        inventory.Add(part);
        
        var inaccessibleDir = new DirectoryDescription
        {
            InventoryPart = part,
            RelativePath = "/blocked",
            IsAccessible = false
        };
        part.DirectoryDescriptions.Add(inaccessibleDir);
        
        return inventory;
    }
    
    private static string CreateInventoryZipFile(string directory, Inventory inventory)
    {
        var zipPath = Path.Combine(directory, $"{Guid.NewGuid()}.zip");
        
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("inventory.json");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream);
        var json = JsonHelper.Serialize(inventory);
        writer.Write(json);
        
        return zipPath;
    }
}
