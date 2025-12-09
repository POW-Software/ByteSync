using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Models.Comparisons;

[TestFixture]
public class ContentIdentityAccessTests
{
    private static (Inventory invA, InventoryPart partA, Inventory invB, InventoryPart partB) CreateInventories()
    {
        var invA = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M1" };
        var invB = new Inventory { InventoryId = "INV_B", Code = "B", Endpoint = new(), MachineName = "M2" };
        var partA = new InventoryPart(invA, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(invB, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        return (invA, partA, invB, partB);
    }
    
    [Test]
    public void HasAccessIssue_ReturnsFalse_WhenAllFilesAccessible()
    {
        var (_, partA, _, _) = CreateInventories();
        
        var ci = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        var file = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        ci.Add(file);
        
        ci.HasAccessIssue.Should().BeFalse();
    }
    
    [Test]
    public void HasAccessIssue_ReturnsTrue_WhenFileIsInaccessible()
    {
        var (_, partA, _, _) = CreateInventories();
        
        var ci = new ContentIdentity(null);
        var file = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            IsAccessible = false
        };
        ci.Add(file);
        
        ci.HasAccessIssue.Should().BeTrue();
    }
    
    [Test]
    public void HasAccessIssue_ReturnsTrue_WhenDirectoryIsInaccessible()
    {
        var (_, partA, _, _) = CreateInventories();
        
        var ci = new ContentIdentity(null);
        var directory = new DirectoryDescription(partA, "/dir") { IsAccessible = false };
        ci.Add(directory);
        
        ci.HasAccessIssue.Should().BeTrue();
    }
    
    [Test]
    public void HasAccessIssue_ReturnsTrue_WhenAccessIssueInventoryPartIsSet()
    {
        var (_, partA, _, partB) = CreateInventories();
        
        var ci = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        var file = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        ci.Add(file);
        
        ci.AddAccessIssue(partB);
        
        ci.HasAccessIssue.Should().BeTrue();
    }
    
    [Test]
    public void HasAccessIssueFor_ReturnsTrue_WhenInventoryPartMarked()
    {
        var (invA, partA, invB, partB) = CreateInventories();
        
        var ci = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        var file = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        ci.Add(file);
        
        ci.AddAccessIssue(partB);
        
        ci.HasAccessIssueFor(invB).Should().BeTrue();
        ci.HasAccessIssueFor(invA).Should().BeFalse();
    }
    
    [Test]
    public void HasAccessIssueFor_ReturnsTrue_WhenFileDescriptionInaccessibleForInventory()
    {
        var (invA, partA, invB, partB) = CreateInventories();
        
        var ci = new ContentIdentity(null);
        
        // Accessible file on A
        var fileA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        ci.Add(fileA);
        
        // Inaccessible file on B
        var fileB = new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file.txt",
            IsAccessible = false
        };
        ci.Add(fileB);
        
        ci.HasAccessIssueFor(invB).Should().BeTrue();
        ci.HasAccessIssueFor(invA).Should().BeFalse();
    }
    
    [Test]
    public void HasAccessIssueFor_ReturnsTrue_WhenDirectoryDescriptionInaccessibleForInventory()
    {
        var (invA, partA, invB, partB) = CreateInventories();
        
        var ci = new ContentIdentity(null);
        
        var directoryA = new DirectoryDescription(partA, "/dir");
        ci.Add(directoryA);
        
        var directoryB = new DirectoryDescription(partB, "/dir") { IsAccessible = false };
        ci.Add(directoryB);
        
        ci.HasAccessIssueFor(invB).Should().BeTrue();
        ci.HasAccessIssueFor(invA).Should().BeFalse();
    }
    
    [Test]
    public void HasAccessIssueFor_ReturnsFalse_WhenInventoryNotPresent()
    {
        var (_, partA, invB, _) = CreateInventories();
        
        var ci = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        var file = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        ci.Add(file);
        
        ci.HasAccessIssueFor(invB).Should().BeFalse();
    }
    
    [Test]
    public void AddAccessIssue_AddsInventoryPartToCollection()
    {
        var (_, _, _, partB) = CreateInventories();
        
        var ci = new ContentIdentity(null);
        
        ci.AccessIssueInventoryParts.Should().BeEmpty();
        
        ci.AddAccessIssue(partB);
        
        ci.AccessIssueInventoryParts.Should().Contain(partB);
        ci.AccessIssueInventoryParts.Should().HaveCount(1);
    }
    
    [Test]
    public void AddAccessIssue_DoesNotDuplicate_WhenCalledMultipleTimes()
    {
        var (_, _, _, partB) = CreateInventories();
        
        var ci = new ContentIdentity(null);
        
        ci.AddAccessIssue(partB);
        ci.AddAccessIssue(partB);
        ci.AddAccessIssue(partB);
        
        ci.AccessIssueInventoryParts.Should().HaveCount(1);
    }
    
    [Test]
    public void IsPresentIn_ReturnsTrue_WhenInventoryPartHasFileDescription()
    {
        var (_, partA, _, _) = CreateInventories();
        
        var ci = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        var file = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        ci.Add(file);
        
        ci.IsPresentIn(partA).Should().BeTrue();
    }
    
    [Test]
    public void IsPresentIn_ReturnsTrue_EvenWhenFileIsInaccessible()
    {
        var (_, partA, _, _) = CreateInventories();
        
        var ci = new ContentIdentity(null);
        var file = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            IsAccessible = false
        };
        ci.Add(file);
        
        ci.IsPresentIn(partA).Should().BeTrue();
    }
    
    [Test]
    public void IsPresentIn_Inventory_ReturnsTrue_WhenAnyPartOfInventoryHasContent()
    {
        var invA = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M1" };
        var part1 = new InventoryPart(invA, "c:/root1", FileSystemTypes.Directory) { Code = "A1" };
        var part2 = new InventoryPart(invA, "c:/root2", FileSystemTypes.Directory) { Code = "A2" };
        
        var ci = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        var file = new FileDescription
        {
            InventoryPart = part2,
            RelativePath = "/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        ci.Add(file);
        
        ci.IsPresentIn(invA).Should().BeTrue();
        ci.IsPresentIn(part1).Should().BeFalse();
        ci.IsPresentIn(part2).Should().BeTrue();
    }
    
    [Test]
    public void GetInventories_ReturnsOnlyInventoriesWithContent()
    {
        var (invA, partA, invB, partB) = CreateInventories();
        
        var ci = new ContentIdentity(new ContentIdentityCore { Size = 100 });
        
        var fileA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = true
        };
        ci.Add(fileA);
        
        var fileB = new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file.txt",
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow,
            IsAccessible = false
        };
        ci.Add(fileB);
        
        var inventories = ci.GetInventories();
        inventories.Should().HaveCount(2);
        inventories.Should().Contain(invA);
        inventories.Should().Contain(invB);
    }
    
    [Test]
    public void GetInventoryParts_ReturnsAllPartsWithContent()
    {
        var (_, partA, _, partB) = CreateInventories();
        
        var ci = new ContentIdentity(null);
        
        var fileA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            IsAccessible = false
        };
        ci.Add(fileA);
        
        var fileB = new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file.txt",
            IsAccessible = true,
            Size = 100,
            CreationTimeUtc = DateTime.UtcNow,
            LastWriteTimeUtc = DateTime.UtcNow
        };
        ci.Add(fileB);
        
        var parts = ci.GetInventoryParts();
        parts.Should().HaveCount(2);
        parts.Should().Contain(partA);
        parts.Should().Contain(partB);
    }
}
