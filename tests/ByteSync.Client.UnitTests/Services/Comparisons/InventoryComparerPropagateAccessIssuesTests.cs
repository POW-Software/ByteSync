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
public class InventoryComparerPropagateAccessIssuesTests
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
    
    private static Inventory CreateInventoryWithInaccessibleDirectory(string inventoryId, string code, string inaccessibleDirPath,
        string fileUnderInaccessibleDir)
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
        
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = $"{code}1" };
        inventory.Add(part);
        
        var inaccessibleDir = new DirectoryDescription
        {
            InventoryPart = part,
            RelativePath = inaccessibleDirPath,
            IsAccessible = false
        };
        part.DirectoryDescriptions.Add(inaccessibleDir);
        
        var accessibleDir = new DirectoryDescription
        {
            InventoryPart = part,
            RelativePath = "/accessible",
            IsAccessible = true
        };
        part.DirectoryDescriptions.Add(accessibleDir);
        
        var fileUnderInaccessible = new FileDescription
        {
            InventoryPart = part,
            RelativePath = fileUnderInaccessibleDir,
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        part.FileDescriptions.Add(fileUnderInaccessible);
        
        return inventory;
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithFileUnderInaccessibleDirectory_DoesNotCreateVirtualContentIdentityWhenFileExists()
    {
        var inventory = CreateInventoryWithInaccessibleDirectory("INV_A", "A", "/inaccessible", "/inaccessible/file.txt");
        var inventoryFile = CreateInventoryZipFile(_tempDirectory, inventory);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFile);
        
        var result = comparer.Compare();
        
        var fileItem = result.ComparisonItems.FirstOrDefault(item => item.PathIdentity.LinkingKeyValue == "/inaccessible/file.txt");
        fileItem.Should().NotBeNull();
        
        fileItem.ContentIdentities.Should().HaveCount(1);
        fileItem.ContentIdentities.All(ci => ci.Core != null).Should().BeTrue();
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithFileUnderInaccessibleDirectory_CreatesVirtualContentIdentityForMissingPart()
    {
        var inventoryA = new Inventory
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
        
        var partA = new InventoryPart(inventoryA, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        inventoryA.Add(partA);
        
        var inaccessibleDirA = new DirectoryDescription
        {
            InventoryPart = partA,
            RelativePath = "/inaccessible",
            IsAccessible = false
        };
        partA.DirectoryDescriptions.Add(inaccessibleDirA);
        
        var fileInA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/inaccessible/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        partA.FileDescriptions.Add(fileInA);
        
        var inventoryB = new Inventory
        {
            InventoryId = "INV_B",
            Code = "B",
            MachineName = "MachineB",
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = "CII_B",
                OSPlatform = OSPlatforms.Windows
            }
        };
        
        var partB = new InventoryPart(inventoryB, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        inventoryB.Add(partB);
        
        var inaccessibleDirB = new DirectoryDescription
        {
            InventoryPart = partB,
            RelativePath = "/inaccessible",
            IsAccessible = false
        };
        partB.DirectoryDescriptions.Add(inaccessibleDirB);
        
        var inventoryFileA = CreateInventoryZipFile(_tempDirectory, inventoryA);
        var inventoryFileB = CreateInventoryZipFile(_tempDirectory, inventoryB);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFileA);
        comparer.AddInventory(inventoryFileB);
        
        var result = comparer.Compare();
        
        var fileItem = result.ComparisonItems.FirstOrDefault(item => item.PathIdentity.LinkingKeyValue == "/inaccessible/file.txt");
        fileItem.Should().NotBeNull();
        
        fileItem.ContentIdentities.Should().HaveCount(2);
        
        var virtualContentIdentity = fileItem.ContentIdentities.FirstOrDefault(ci => ci.Core == null);
        virtualContentIdentity.Should().NotBeNull();
        virtualContentIdentity.AccessIssueInventoryParts.Should().Contain(partB);
        
        var virtualFileDescription = virtualContentIdentity.FileSystemDescriptions
            .OfType<FileDescription>()
            .FirstOrDefault(fd => !fd.IsAccessible);
        virtualFileDescription.Should().NotBeNull();
        virtualFileDescription.RelativePath.Should().Be("/inaccessible/file.txt");
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithDirectoryUnderInaccessibleDirectory_MarksExistingContentIdentity()
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
        
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        inventory.Add(part);
        
        var inaccessibleDir = new DirectoryDescription
        {
            InventoryPart = part,
            RelativePath = "/inaccessible",
            IsAccessible = false
        };
        part.DirectoryDescriptions.Add(inaccessibleDir);
        
        var subDir = new DirectoryDescription
        {
            InventoryPart = part,
            RelativePath = "/inaccessible/subdir",
            IsAccessible = true
        };
        part.DirectoryDescriptions.Add(subDir);
        
        var inventoryFile = CreateInventoryZipFile(_tempDirectory, inventory);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFile);
        
        var result = comparer.Compare();
        
        var dirItem = result.ComparisonItems.FirstOrDefault(item => item.PathIdentity.LinkingKeyValue == "/inaccessible/subdir");
        dirItem.Should().NotBeNull();
        
        dirItem.ContentIdentities.Should().HaveCount(1);
        var contentIdentity = dirItem.ContentIdentities.First();
        contentIdentity.AccessIssueInventoryParts.Should().Contain(part);
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithFileNotUnderInaccessibleDirectory_DoesNotCreateVirtualContentIdentity()
    {
        var inventory = CreateInventoryWithInaccessibleDirectory("INV_A", "A", "/inaccessible", "/accessible/file.txt");
        var inventoryFile = CreateInventoryZipFile(_tempDirectory, inventory);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFile);
        
        var result = comparer.Compare();
        
        var fileItem = result.ComparisonItems.FirstOrDefault(item => item.PathIdentity.LinkingKeyValue == "/accessible/file.txt");
        fileItem.Should().NotBeNull();
        
        fileItem.ContentIdentities.Should().HaveCount(1);
        fileItem.ContentIdentities.All(ci => ci.Core != null).Should().BeTrue();
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithFileAlreadyHavingContentForPart_DoesNotCreateDuplicate()
    {
        var inventory = CreateInventoryWithInaccessibleDirectory("INV_A", "A", "/inaccessible", "/inaccessible/file.txt");
        var inventoryFile = CreateInventoryZipFile(_tempDirectory, inventory);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFile);
        
        var result = comparer.Compare();
        
        var fileItem = result.ComparisonItems.FirstOrDefault(item => item.PathIdentity.LinkingKeyValue == "/inaccessible/file.txt");
        fileItem.Should().NotBeNull();
        
        var contentIdentitiesForPart = fileItem.ContentIdentities
            .Where(ci => ci.GetInventoryParts().Contains(inventory.InventoryParts[0]))
            .ToList();
        
        contentIdentitiesForPart.Should().HaveCount(1);
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithEmptyPath_IgnoresItem()
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
        
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        inventory.Add(part);
        
        var inaccessibleDir = new DirectoryDescription
        {
            InventoryPart = part,
            RelativePath = "/inaccessible",
            IsAccessible = false
        };
        part.DirectoryDescriptions.Add(inaccessibleDir);
        
        var inventoryFile = CreateInventoryZipFile(_tempDirectory, inventory);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFile);
        
        var result = comparer.Compare();
        
        result.ComparisonItems.Should().NotContain(item => string.IsNullOrWhiteSpace(item.PathIdentity.LinkingKeyValue));
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithRootPath_IgnoresItem()
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
        
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        inventory.Add(part);
        
        var inaccessibleDir = new DirectoryDescription
        {
            InventoryPart = part,
            RelativePath = "/inaccessible",
            IsAccessible = false
        };
        part.DirectoryDescriptions.Add(inaccessibleDir);
        
        var rootDir = new DirectoryDescription
        {
            InventoryPart = part,
            RelativePath = "/",
            IsAccessible = true
        };
        part.DirectoryDescriptions.Add(rootDir);
        
        var inventoryFile = CreateInventoryZipFile(_tempDirectory, inventory);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFile);
        
        var result = comparer.Compare();
        
        var rootItem = result.ComparisonItems.FirstOrDefault(item => item.PathIdentity.LinkingKeyValue == "/");
        if (rootItem != null)
        {
            rootItem.ContentIdentities.Should().NotContain(ci => ci.AccessIssueInventoryParts.Any());
        }
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithNestedInaccessibleAncestors_DetectsCorrectly()
    {
        var inventoryA = new Inventory
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
        
        var partA = new InventoryPart(inventoryA, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        inventoryA.Add(partA);
        
        var inaccessibleDir1 = new DirectoryDescription
        {
            InventoryPart = partA,
            RelativePath = "/inaccessible1",
            IsAccessible = false
        };
        partA.DirectoryDescriptions.Add(inaccessibleDir1);
        
        var inaccessibleDir2 = new DirectoryDescription
        {
            InventoryPart = partA,
            RelativePath = "/inaccessible1/inaccessible2",
            IsAccessible = false
        };
        partA.DirectoryDescriptions.Add(inaccessibleDir2);
        
        var fileUnderNested = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/inaccessible1/inaccessible2/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        partA.FileDescriptions.Add(fileUnderNested);
        
        var inventoryB = new Inventory
        {
            InventoryId = "INV_B",
            Code = "B",
            MachineName = "MachineB",
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = "CII_B",
                OSPlatform = OSPlatforms.Windows
            }
        };
        
        var partB = new InventoryPart(inventoryB, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        inventoryB.Add(partB);
        
        var inaccessibleDir1B = new DirectoryDescription
        {
            InventoryPart = partB,
            RelativePath = "/inaccessible1",
            IsAccessible = false
        };
        partB.DirectoryDescriptions.Add(inaccessibleDir1B);
        
        var inventoryFileA = CreateInventoryZipFile(_tempDirectory, inventoryA);
        var inventoryFileB = CreateInventoryZipFile(_tempDirectory, inventoryB);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFileA);
        comparer.AddInventory(inventoryFileB);
        
        var result = comparer.Compare();
        
        var fileItem =
            result.ComparisonItems.FirstOrDefault(item => item.PathIdentity.LinkingKeyValue == "/inaccessible1/inaccessible2/file.txt");
        fileItem.Should().NotBeNull();
        
        fileItem.ContentIdentities.Should().HaveCount(2);
        
        var virtualContentIdentity = fileItem.ContentIdentities.FirstOrDefault(ci => ci.Core == null);
        virtualContentIdentity.Should().NotBeNull();
        virtualContentIdentity.AccessIssueInventoryParts.Should().Contain(partB);
    }
    
    [Test]
    public void AddIncompleteParts_WithFlatMode_AddsVirtualDirectoryForIncompleteInventory()
    {
        // Inventory A: accessible directory present
        var inventoryA = new Inventory
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
        var partA = new InventoryPart(inventoryA, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        inventoryA.Add(partA);
        partA.DirectoryDescriptions.Add(new DirectoryDescription
        {
            InventoryPart = partA,
            RelativePath = "/bytesync-test"
        });
        
        // Inventory B: incomplete, no directory content
        var inventoryB = new Inventory
        {
            InventoryId = "INV_B",
            Code = "B",
            MachineName = "MachineB",
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = "CII_B",
                OSPlatform = OSPlatforms.Windows
            }
        };
        var partB = new InventoryPart(inventoryB, "c:/rootB", FileSystemTypes.Directory)
        {
            Code = "B1",
            IsIncompleteDueToAccess = true
        };
        inventoryB.Add(partB);
        
        var inventoryFileA = CreateInventoryZipFile(_tempDirectory, inventoryA);
        var inventoryFileB = CreateInventoryZipFile(_tempDirectory, inventoryB);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Flat,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFileA);
        comparer.AddInventory(inventoryFileB);
        
        var result = comparer.Compare();
        
        var dirItem = result.ComparisonItems.FirstOrDefault(item =>
            item.PathIdentity.FileName.Equals("bytesync-test", StringComparison.Ordinal));
        dirItem.Should().NotBeNull();
        
        // Expect directory content to include the incomplete inventory part B
        dirItem.ContentIdentities.Should().HaveCount(1);
        var contentForB = dirItem.ContentIdentities.First();
        
        var inventoryBFromResult = result.Inventories.First(inv => inv.InventoryId == "INV_B");
        var partBFromResult = inventoryBFromResult.InventoryParts.First(ip => ip.Code == "B1");
        
        contentForB.AccessIssueInventoryParts.Should().Contain(partBFromResult);
        contentForB.FileSystemDescriptions.OfType<DirectoryDescription>()
            .Any(d => Equals(d.InventoryPart, partBFromResult) && !d.IsAccessible)
            .Should().BeTrue();
    }
    
    [Test]
    public void AddIncompleteParts_WithFlatMode_ExistingDirectoryOnBothSides_MarksIncompleteInventory()
    {
        // Inventory A: directory present
        var inventoryA = new Inventory
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
        var partA = new InventoryPart(inventoryA, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        inventoryA.Add(partA);
        partA.DirectoryDescriptions.Add(new DirectoryDescription
        {
            InventoryPart = partA,
            RelativePath = "/bytesync-test"
        });
        
        // Inventory B: same directory listed but inventory incomplete
        var inventoryB = new Inventory
        {
            InventoryId = "INV_B",
            Code = "B",
            MachineName = "MachineB",
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = "CII_B",
                OSPlatform = OSPlatforms.Windows
            }
        };
        var partB = new InventoryPart(inventoryB, "c:/rootB", FileSystemTypes.Directory)
        {
            Code = "B1",
            IsIncompleteDueToAccess = true
        };
        inventoryB.Add(partB);
        partB.DirectoryDescriptions.Add(new DirectoryDescription
        {
            InventoryPart = partB,
            RelativePath = "/bytesync-test"
        });
        
        var inventoryFileA = CreateInventoryZipFile(_tempDirectory, inventoryA);
        var inventoryFileB = CreateInventoryZipFile(_tempDirectory, inventoryB);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Flat,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFileA);
        comparer.AddInventory(inventoryFileB);
        
        var result = comparer.Compare();
        
        var dirItem = result.ComparisonItems.FirstOrDefault(item =>
            item.PathIdentity.FileName.Equals("bytesync-test", StringComparison.Ordinal));
        dirItem.Should().NotBeNull();
        
        // Should have a single ContentIdentity containing both parts, with access issue flagged for B
        dirItem.ContentIdentities.Should().HaveCount(1);
        var content = dirItem.ContentIdentities.First();
        
        var inventoryBFromResult = result.Inventories.First(inv => inv.InventoryId == "INV_B");
        var partBFromResult = inventoryBFromResult.InventoryParts.First(ip => ip.Code == "B1");
        
        content.AccessIssueInventoryParts.Should().Contain(partBFromResult);
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithMultipleInventories_PropagatesPerInventoryPart()
    {
        var inventoryA = new Inventory
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
        
        var partA = new InventoryPart(inventoryA, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        inventoryA.Add(partA);
        
        var inaccessibleDirA = new DirectoryDescription
        {
            InventoryPart = partA,
            RelativePath = "/inaccessible",
            IsAccessible = false
        };
        partA.DirectoryDescriptions.Add(inaccessibleDirA);
        
        var fileInA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/inaccessible/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        partA.FileDescriptions.Add(fileInA);
        
        var inventoryB = new Inventory
        {
            InventoryId = "INV_B",
            Code = "B",
            MachineName = "MachineB",
            Endpoint = new ByteSyncEndpoint
            {
                ClientInstanceId = "CII_B",
                OSPlatform = OSPlatforms.Windows
            }
        };
        
        var partB = new InventoryPart(inventoryB, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        inventoryB.Add(partB);
        
        var inaccessibleDirB = new DirectoryDescription
        {
            InventoryPart = partB,
            RelativePath = "/inaccessible",
            IsAccessible = false
        };
        partB.DirectoryDescriptions.Add(inaccessibleDirB);
        
        var inventoryFileA = CreateInventoryZipFile(_tempDirectory, inventoryA);
        var inventoryFileB = CreateInventoryZipFile(_tempDirectory, inventoryB);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Tree,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFileA);
        comparer.AddInventory(inventoryFileB);
        
        var result = comparer.Compare();
        
        var fileItem = result.ComparisonItems.FirstOrDefault(item => item.PathIdentity.LinkingKeyValue == "/inaccessible/file.txt");
        fileItem.Should().NotBeNull();
        fileItem.ContentIdentities.Should().HaveCount(2);
        
        var virtualContentIdentity = fileItem.ContentIdentities.FirstOrDefault(ci => ci.Core == null);
        virtualContentIdentity.Should().NotBeNull();
        virtualContentIdentity.AccessIssueInventoryParts.Should().Contain(partB);
    }
    
    [Test]
    public void PropagateAccessIssuesFromAncestors_WithFlatMatchingMode_DoesNotPropagate()
    {
        var inventory = CreateInventoryWithInaccessibleDirectory("INV_A", "A", "/inaccessible", "/inaccessible/file.txt");
        var inventoryFile = CreateInventoryZipFile(_tempDirectory, inventory);
        
        var sessionSettings = new SessionSettings
        {
            DataType = DataTypes.FilesDirectories,
            MatchingMode = MatchingModes.Flat,
            LinkingCase = LinkingCases.Insensitive
        };
        
        var initialStatusBuilder = new InitialStatusBuilder();
        using var comparer = new InventoryComparer(sessionSettings, initialStatusBuilder);
        comparer.AddInventory(inventoryFile);
        
        var result = comparer.Compare();
        
        var fileItem = result.ComparisonItems.FirstOrDefault(item =>
            item.PathIdentity.LinkingKeyValue == "/inaccessible/file.txt" ||
            item.PathIdentity.FileName.Equals("file.txt", StringComparison.Ordinal));
        if (fileItem != null)
        {
            fileItem.ContentIdentities.Should().HaveCount(1);
            fileItem.ContentIdentities.All(ci => ci.Core != null).Should().BeTrue();
        }
    }
}