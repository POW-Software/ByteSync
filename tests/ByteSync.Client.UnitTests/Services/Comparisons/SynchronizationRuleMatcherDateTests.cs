using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Comparisons.ConditionMatchers;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class SynchronizationRuleMatcherDateTests
{
    private DateConditionMatcher _matcher = null!;
    
    [SetUp]
    public void SetUp()
    {
        var extractor = new ContentIdentityExtractor();
        _matcher = new DateConditionMatcher(extractor);
    }
    
    [Test]
    public void ConditionMatchesDate_Equals_WithSameDate_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var date = DateTime.UtcNow;
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescriptionA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = date
        };
        var fileDescriptionB = new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = date
        };
        contentIdentity.Add(fileDescriptionA);
        contentIdentity.Add(fileDescriptionB);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesDate_Equals_WithDifferentDate_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var dateA = DateTime.UtcNow;
        var dateB = dateA.AddHours(1);
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescriptionA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateA
        };
        var fileDescriptionB = new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateB
        };
        contentIdentity.Add(fileDescriptionA);
        contentIdentity.Add(fileDescriptionB);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesDate_ReturnsFalse_WhenDestinationPartIncomplete()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1", IsIncompleteDueToAccess = true };
        
        var now = DateTime.UtcNow;
        var ciA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 1 });
        ciA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt", LastWriteTimeUtc = now });
        var ciB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 1 });
        ciB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt", LastWriteTimeUtc = now });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(ciA);
        comparisonItem.AddContentIdentity(ciB);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesDate_NotEquals_WithDifferentDate_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var dateA = DateTime.UtcNow;
        var dateB = dateA.AddHours(1);
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescriptionA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateA
        };
        var fileDescriptionB = new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateB
        };
        contentIdentity.Add(fileDescriptionA);
        contentIdentity.Add(fileDescriptionB);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.NotEquals
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesDate_IsNewerThan_WithNewerSource_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var dateA = DateTime.UtcNow;
        var dateB = dateA.AddHours(-1);
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescriptionA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateA
        };
        var fileDescriptionB = new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateB
        };
        contentIdentity.Add(fileDescriptionA);
        contentIdentity.Add(fileDescriptionB);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.IsNewerThan
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesDate_IsOlderThan_WithOlderSource_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var dateA = DateTime.UtcNow.AddHours(-1);
        var dateB = DateTime.UtcNow;
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescriptionA = new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateA
        };
        var fileDescriptionB = new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateB
        };
        contentIdentity.Add(fileDescriptionA);
        contentIdentity.Add(fileDescriptionB);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.IsOlderThan
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesDate_WithNullSourceDate_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 100 });
        contentIdentityB.Add(new FileDescription
        {
            InventoryPart = partB,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = DateTime.UtcNow
        });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesDate_WithVirtualDestination_UsesDateTime()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        
        var dateA = DateTime.UtcNow;
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 100 });
        contentIdentityA.Add(new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateA
        });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        
        var virtualDestination = new DataPart("A");
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = virtualDestination,
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.Equals,
            DateTime = dateA
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesDate_WithVirtualDestination_TrimsSecondsAndMilliseconds()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        
        var dateA = new DateTime(2024, 1, 1, 12, 30, 0, 0, DateTimeKind.Utc);
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 100 });
        contentIdentityA.Add(new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateA
        });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        
        var virtualDestination = new DataPart("A");
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = virtualDestination,
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.Equals,
            DateTime = dateA
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesDate_IsNewerThan_WithVirtualDestinationNull_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        
        var dateA = DateTime.UtcNow;
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 100 });
        contentIdentityA.Add(new FileDescription
        {
            InventoryPart = partA,
            RelativePath = "/file.txt",
            LastWriteTimeUtc = dateA
        });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        
        var virtualDestination = new DataPart("A");
        var olderDate = dateA.AddHours(-1);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = virtualDestination,
            ComparisonProperty = ComparisonProperty.Date,
            ConditionOperator = ConditionOperatorTypes.IsNewerThan,
            DateTime = olderDate
        };
        
        var result = _matcher.Matches(condition, comparisonItem);
        
        result.Should().BeTrue();
    }
}
