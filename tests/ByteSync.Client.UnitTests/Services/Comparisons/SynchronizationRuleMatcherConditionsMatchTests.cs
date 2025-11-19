using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Comparisons.ConditionMatchers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class SynchronizationRuleMatcherConditionsMatchTests
{
    private Mock<IAtomicActionConsistencyChecker> _consistencyCheckerMock = null!;
    private Mock<IAtomicActionRepository> _repositoryMock = null!;
    private SynchronizationRuleMatcher _matcher = null!;
    
    [SetUp]
    public void SetUp()
    {
        _consistencyCheckerMock = new Mock<IAtomicActionConsistencyChecker>();
        _repositoryMock = new Mock<IAtomicActionRepository>();
        
        var extractor = new ContentIdentityExtractor();
        var matchers = new IConditionMatcher[]
        {
            new ContentConditionMatcher(extractor),
            new SizeConditionMatcher(extractor),
            new DateConditionMatcher(extractor),
            new PresenceConditionMatcher(extractor),
            new NameConditionMatcher()
        };
        var factory = new ConditionMatcherFactory(matchers);
        
        _matcher = new SynchronizationRuleMatcher(_consistencyCheckerMock.Object, _repositoryMock.Object, factory);
    }
    
    [Test]
    public void ConditionsMatch_WithEmptyConditions_ReturnsFalse()
    {
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var result = _matcher.ConditionsMatch(rule, comparisonItem);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionsMatch_WithDifferentFileSystemType_ReturnsFalse()
    {
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
        var condition = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "file.txt"
        };
        rule.Conditions.Add(condition);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.Directory, "/dir", "dir", "/dir"));
        
        var result = _matcher.ConditionsMatch(rule, comparisonItem);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionsMatch_WithConditionModeAll_AllConditionsMustMatch()
    {
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
        var condition1 = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "file.txt"
        };
        var condition2 = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "file.txt"
        };
        rule.Conditions.Add(condition1);
        rule.Conditions.Add(condition2);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var result = _matcher.ConditionsMatch(rule, comparisonItem);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionsMatch_WithConditionModeAll_OneConditionFails_ReturnsFalse()
    {
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
        var condition1 = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "file.txt"
        };
        var condition2 = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "other.txt"
        };
        rule.Conditions.Add(condition1);
        rule.Conditions.Add(condition2);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var result = _matcher.ConditionsMatch(rule, comparisonItem);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionsMatch_WithConditionModeAny_OneConditionMatches_ReturnsTrue()
    {
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.Any);
        var condition1 = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "file.txt"
        };
        var condition2 = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "other.txt"
        };
        rule.Conditions.Add(condition1);
        rule.Conditions.Add(condition2);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var result = _matcher.ConditionsMatch(rule, comparisonItem);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void ConditionsMatch_WithConditionModeAny_NoConditionMatches_ReturnsFalse()
    {
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.Any);
        var condition1 = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "other1.txt"
        };
        var condition2 = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "other2.txt"
        };
        rule.Conditions.Add(condition1);
        rule.Conditions.Add(condition2);
        
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var result = _matcher.ConditionsMatch(rule, comparisonItem);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void ConditionMatches_WithUnknownComparisonProperty_ReturnsFalse()
    {
        var condition = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = (ComparisonProperty)999,
            ConditionOperator = ConditionOperatorTypes.Equals
        };
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        
        var factory = SynchronizationRuleMatcherTestHelper.CreateConditionMatcherFactory();
        var matcher = factory.GetMatcher(condition.ComparisonProperty);
        var result = matcher.Matches(condition, comparisonItem);
        
        result.Should().BeFalse();
    }
}