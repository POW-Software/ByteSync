using System.IO.Compression;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Controls.Json;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class InventoryComparerIncompletePartsFlatTests
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
    public void Compare_Flat_WithIncompletePart_AddsAccessIssuePlaceholderAndSkipsPartContent()
    {
        var inventoryA = CreateInventoryWithFile("INV_A", "A", "/file.txt", false);
        var inventoryB = CreateInventoryWithFile("INV_B", "B", "/file.txt", true);
        
        var inventoryFileA = CreateInventoryZipFile(_tempDirectory, inventoryA);
        var inventoryFileB = CreateInventoryZipFile(_tempDirectory, inventoryB);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.Files,
            MatchingMode = MatchingModes.Flat,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFileA);
        comparer.AddInventory(inventoryFileB);
        
        var result = comparer.Compare();
        
        result.ComparisonItems.Should().HaveCount(1);
        var item = result.ComparisonItems.Single();
        item.PathIdentity.LinkingKeyValue.Should().Be("file.txt");
        
        var inventoryAResult = result.Inventories.Single(i => i.Code == "A");
        var inventoryBResult = result.Inventories.Single(i => i.Code == "B");
        var partA = inventoryAResult.InventoryParts.Single();
        var partB = inventoryBResult.InventoryParts.Single();
        
        item.ContentIdentities.Should().HaveCount(2);
        
        var contentIdentityA = item.ContentIdentities.Single(ci => ci.IsPresentIn(partA));
        contentIdentityA.Core.Should().NotBeNull();
        
        var contentIdentityB = item.ContentIdentities.Single(ci => ci.IsPresentIn(partB));
        contentIdentityB.Core.Should().BeNull();
        contentIdentityB.HasAccessIssue.Should().BeTrue();
        contentIdentityB.AccessIssueInventoryParts.Should().Contain(partB);
        
        var placeholderFileDescription = contentIdentityB.GetFileSystemDescriptions(partB).OfType<FileDescription>().Single();
        placeholderFileDescription.IsAccessible.Should().BeFalse();
        placeholderFileDescription.RelativePath.Should().Be("/file.txt");
    }
    
    [Test]
    public void Compare_Flat_AllPartsIncomplete_ReturnsNoItems()
    {
        var inventoryB = CreateInventoryWithFile("INV_B", "B", "/file.txt", true);
        var inventoryFileB = CreateInventoryZipFile(_tempDirectory, inventoryB);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.Files,
            MatchingMode = MatchingModes.Flat,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFileB);
        
        var result = comparer.Compare();
        
        result.ComparisonItems.Should().BeEmpty();
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
    
    private static Inventory CreateInventoryWithFile(string inventoryId, string code, string relativeFilePath, bool isIncomplete)
    {
        var inventory = new Inventory
        {
            InventoryId = inventoryId,
            Code = code,
            MachineName = $"Machine{code}",
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = $"CII_{code}",
                OSPlatform = OSPlatforms.Windows
            }
        };
        
        var part = new InventoryPart(inventory, $"c:/root{code}", FileSystemTypes.Directory)
        {
            Code = $"{code}1",
            IsIncompleteDueToAccess = isIncomplete
        };
        inventory.Add(part);
        
        if (isIncomplete)
        {
            var blockedDirectory = new DirectoryDescription
            {
                InventoryPart = part,
                RelativePath = "/blocked",
                IsAccessible = false
            };
            part.DirectoryDescriptions.Add(blockedDirectory);
        }
        
        var fileDescription = new FileDescription
        {
            InventoryPart = part,
            RelativePath = relativeFilePath,
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        part.FileDescriptions.Add(fileDescription);
        
        return inventory;
    }
}
