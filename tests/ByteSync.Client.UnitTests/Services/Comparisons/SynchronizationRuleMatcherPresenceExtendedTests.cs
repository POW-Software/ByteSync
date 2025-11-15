using System.Reflection;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
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
public class SynchronizationRuleMatcherPresenceExtendedTests
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
    public void ConditionMatchesPresence_ExistsOn_WithBothPresent_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        contentIdentity.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentity.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Presence,
            ConditionOperator = ConditionOperatorTypes.ExistsOn
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesPresence", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesPresence_ExistsOn_WithSourceNotPresent_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        contentIdentity.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Presence,
            ConditionOperator = ConditionOperatorTypes.ExistsOn
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesPresence", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesPresence_NotExistsOn_WithSourcePresentDestinationNotPresent_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        contentIdentity.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Presence,
            ConditionOperator = ConditionOperatorTypes.NotExistsOn
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesPresence", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesPresence_NotExistsOn_WithBothPresent_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        contentIdentity.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentity.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Presence,
            ConditionOperator = ConditionOperatorTypes.NotExistsOn
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesPresence", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesPresence_WithUnknownOperator_ThrowsArgumentOutOfRangeException()
    {
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        var condition = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Presence,
            ConditionOperator = (ConditionOperatorTypes)999
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesPresence", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var act = () => method.Invoke(_matcher, new object[] { condition, comparisonItem });
        
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentOutOfRangeException>()
            .WithMessage("*ConditionMatchesPresence*");
    }
    
    [Test]
    public void ExistsOn_WithNullDataPart_ReturnsFalse()
    {
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ExistsOn", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { null, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ExistsOn_WithDirectory_ReturnsTrueWhenContentIdentityExists()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        var contentIdentity = new ContentIdentity(null);
        contentIdentity.Add(new DirectoryDescription { InventoryPart = part, RelativePath = "/dir" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.Directory, "/dir", "dir", "/dir"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var dataPart = new DataPart("A", part);
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ExistsOn", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { dataPart, comparisonItem })!;
        
        result.Should().BeTrue();
    }
}