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
public class SynchronizationRuleMatcherContentTests
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
    public void ConditionMatchesContent_WithDirectory_ReturnsFalse()
    {
        var condition = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.Directory, "/dir", "dir", "/dir"));
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesContent_WithContentIdentityHasAnalysisError_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescription = new FileDescription
        {
            InventoryPart = part,
            RelativePath = "/file.txt",
            AnalysisErrorDescription = "Test error"
        };
        contentIdentity.Add(fileDescription);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", part),
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesContent_WithContentIdentityHasAccessIssue_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        contentIdentity.Add(new FileDescription { InventoryPart = part, RelativePath = "/file.txt" });
        contentIdentity.AddAccessIssue(part);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", part),
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesContent_Equals_WithBothNull_ReturnsTrue()
    {
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        var condition = new AtomicCondition
        {
            Source = null,
            Destination = null,
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesContent_Equals_WithSourceNullDestinationNotNull_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescription = new FileDescription { InventoryPart = part, RelativePath = "/file.txt" };
        contentIdentity.Add(fileDescription);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = null,
            Destination = new DataPart("A", part),
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesContent_Equals_WithSourceNotNullDestinationNull_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        var fileDescription = new FileDescription { InventoryPart = part, RelativePath = "/file.txt" };
        contentIdentity.Add(fileDescription);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", part),
            Destination = null,
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesContent_Equals_WithSameHash_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash123", Size = 100 });
        var fileDescriptionA = new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" };
        var fileDescriptionB = new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" };
        contentIdentity.Add(fileDescriptionA);
        contentIdentity.Add(fileDescriptionB);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesContent_Equals_WithDifferentHash_ReturnsFalse()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 100 });
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 100 });
        contentIdentityA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentityB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatchesContent_NotEquals_WithSourceNullDestinationNotNull_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var part = new InventoryPart(inventory, "c:/root", FileSystemTypes.Directory) { Code = "A1" };
        var contentIdentity = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hash1", Size = 100 });
        contentIdentity.Add(new FileDescription { InventoryPart = part, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentity);
        
        var condition = new AtomicCondition
        {
            Source = null,
            Destination = new DataPart("A", part),
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.NotEquals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesContent_NotEquals_WithDifferentHash_ReturnsTrue()
    {
        var inventory = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = new(), MachineName = "M" };
        var partA = new InventoryPart(inventory, "c:/rootA", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventory, "c:/rootB", FileSystemTypes.Directory) { Code = "B1" };
        
        var contentIdentityA = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashA", Size = 100 });
        var contentIdentityB = new ContentIdentity(new ContentIdentityCore { SignatureHash = "hashB", Size = 100 });
        contentIdentityA.Add(new FileDescription { InventoryPart = partA, RelativePath = "/file.txt" });
        contentIdentityB.Add(new FileDescription { InventoryPart = partB, RelativePath = "/file.txt" });
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        comparisonItem.AddContentIdentity(contentIdentityA);
        comparisonItem.AddContentIdentity(contentIdentityB);
        
        var condition = new AtomicCondition
        {
            Source = new DataPart("A", partA),
            Destination = new DataPart("A", partB),
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = ConditionOperatorTypes.NotEquals
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(_matcher, new object[] { condition, comparisonItem })!;
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionMatchesContent_WithUnknownOperator_ThrowsArgumentOutOfRangeException()
    {
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        var condition = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Content,
            ConditionOperator = (ConditionOperatorTypes)999
        };
        
        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesContent", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var act = () => method.Invoke(_matcher, new object[] { condition, comparisonItem });
        
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentOutOfRangeException>()
            .WithMessage("*ConditionMatchesContent*");
    }
}