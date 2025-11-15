using System.Reflection;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
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
public class SynchronizationRuleMatcherSizeTests
{
    private Mock<IAtomicActionConsistencyChecker> _consistencyCheckerMock;
    private Mock<IAtomicActionRepository> _repositoryMock;
    private SynchronizationRuleMatcher _matcher;
    
    [SetUp]
    public void SetUp()
    {
        _consistencyCheckerMock = new Mock<IAtomicActionConsistencyChecker>();
        _repositoryMock = new Mock<IAtomicActionRepository>();
        _matcher = new SynchronizationRuleMatcher(_consistencyCheckerMock.Object, _repositoryMock.Object);
    }
    
    [Test]
    public void ConditionMatchesSize_Equals_WithSameSize_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 1024 });
        contentIdentity.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentity.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Size,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesSize", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesSize_Equals_WithDifferentSize_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 1024 });
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 2048 });
        contentIdentityA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentityB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Size,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesSize", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesSize_NotEquals_WithDifferentSize_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 1024 });
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 2048 });
        contentIdentityA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentityB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Size,
            ConditionOperator = ConditionOperatorTypes.NotEquals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesSize", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesSize_IsSmallerThan_WithSmallerSize_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 1024 });
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 2048 });
        contentIdentityA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentityB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Size,
            ConditionOperator = ConditionOperatorTypes.IsSmallerThan
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesSize", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesSize_IsBiggerThan_WithBiggerSize_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 2048 });
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 1024 });
        contentIdentityA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentityB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Size,
            ConditionOperator = ConditionOperatorTypes.IsBiggerThan
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesSize", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesSize_WithNullSizeSource_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 1024 });
        contentIdentityB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Size,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesSize", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesSize_WithVirtualDestination_UsesSizeAndSizeUnit()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 1024 });
        contentIdentityA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        
        var virtualDestination = new DataPart("A");
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = virtualDestination,
            ComparisonProperty = ComparisonProperty.Size,
            ConditionOperator = ConditionOperatorTypes.Equals,
            Size = 1,
            SizeUnit = SizeUnits.KB
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesSize", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
}