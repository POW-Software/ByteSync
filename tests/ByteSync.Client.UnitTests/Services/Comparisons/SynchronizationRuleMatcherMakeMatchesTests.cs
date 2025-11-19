using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Comparisons.ConditionMatchers;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class SynchronizationRuleMatcherMakeMatchesTests
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
    public void MakeMatches_WithSingleComparisonItem_CallsRepositoryAddOrUpdate()
    {
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        var rules = new List<SynchronizationRule>();
        
        _repositoryMock.Setup(r => r.GetAtomicActions(comparisonItem)).Returns([]);
        _consistencyCheckerMock.Setup(c => c.GetApplicableActions(It.IsAny<ICollection<SynchronizationRule>>()))
            .Returns([]);
        
        _matcher.MakeMatches(comparisonItem, rules);
        
        _repositoryMock.Verify(r => r.AddOrUpdate(It.IsAny<HashSet<AtomicAction>>()), Times.Once);
    }
    
    [Test]
    public void MakeMatches_WithCollectionOfComparisonItems_CallsRepositoryAddOrUpdateOnce()
    {
        var comparisonItems = new List<ComparisonItem>
        {
            new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "/file1.txt")),
            new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file2.txt", "file2.txt", "/file2.txt"))
        };
        var rules = new List<SynchronizationRule>();
        
        _repositoryMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns([]);
        _consistencyCheckerMock.Setup(c => c.GetApplicableActions(It.IsAny<ICollection<SynchronizationRule>>()))
            .Returns([]);
        
        _matcher.MakeMatches(comparisonItems, rules);
        
        _repositoryMock.Verify(r => r.AddOrUpdate(It.IsAny<HashSet<AtomicAction>>()), Times.Once);
    }
    
    [Test]
    public void MakeMatches_RemovesExistingRuleActions_BeforeAddingNewOnes()
    {
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        var rules = new List<SynchronizationRule>();
        
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
        var existingRuleAction = new AtomicAction { SynchronizationRule = rule };
        var existingNonRuleAction = new AtomicAction();
        
        _repositoryMock.Setup(r => r.GetAtomicActions(comparisonItem))
            .Returns([existingRuleAction, existingNonRuleAction]);
        _consistencyCheckerMock.Setup(c => c.GetApplicableActions(It.IsAny<ICollection<SynchronizationRule>>()))
            .Returns([]);
        
        _matcher.MakeMatches(comparisonItem, rules);
        
        _repositoryMock.Verify(r => r.Remove(It.Is<List<AtomicAction>>(actions =>
            actions.Count == 1 && actions.Contains(existingRuleAction))), Times.Once);
    }
    
    [Test]
    public void MakeMatches_WithMatchingRule_AddsActionsThatPassConsistencyCheck()
    {
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
        var condition = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "file.txt"
        };
        rule.Conditions.Add(condition);
        
        var action1 = new AtomicAction { Operator = ActionOperatorTypes.DoNothing };
        var action2 = new AtomicAction { Operator = ActionOperatorTypes.Create };
        
        _repositoryMock.Setup(r => r.GetAtomicActions(comparisonItem)).Returns([]);
        _consistencyCheckerMock.Setup(c => c.GetApplicableActions(It.IsAny<ICollection<SynchronizationRule>>()))
            .Returns([action1, action2]);
        _consistencyCheckerMock.Setup(c =>
                c.CheckCanAdd(It.Is<AtomicAction>(a => a.Operator == ActionOperatorTypes.DoNothing), comparisonItem))
            .Returns(new AtomicActionConsistencyCheckCanAddResult(new List<ComparisonItem> { comparisonItem })
            {
                ValidationResults = [new ComparisonItemValidationResult(comparisonItem, true)]
            });
        _consistencyCheckerMock
            .Setup(c => c.CheckCanAdd(It.Is<AtomicAction>(a => a.Operator == ActionOperatorTypes.Create), comparisonItem))
            .Returns(new AtomicActionConsistencyCheckCanAddResult(new List<ComparisonItem> { comparisonItem })
            {
                ValidationResults =
                [
                    new ComparisonItemValidationResult(comparisonItem,
                        AtomicActionValidationFailureReason.SourceNotAllowedForCreateOperation)
                ]
            });
        
        _matcher.MakeMatches(comparisonItem, new List<SynchronizationRule> { rule });
        
        _repositoryMock.Verify(r => r.AddOrUpdate(It.Is<HashSet<AtomicAction>>(actions =>
            actions.Count == 1 && actions.Any(a => a.Operator == ActionOperatorTypes.DoNothing))), Times.Once);
    }
    
    [Test]
    public void MakeMatches_ClonesActionsBeforeCheckingConsistency()
    {
        var comparisonItem = new ComparisonItem(new PathIdentity(FileSystemTypes.File, "/file.txt", "file.txt", "/file.txt"));
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
        var condition = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = ConditionOperatorTypes.Equals,
            NamePattern = "file.txt"
        };
        rule.Conditions.Add(condition);
        
        var originalAction = new AtomicAction { Operator = ActionOperatorTypes.DoNothing };
        originalAction.ComparisonItem = comparisonItem;
        
        _repositoryMock.Setup(r => r.GetAtomicActions(comparisonItem)).Returns([]);
        _consistencyCheckerMock.Setup(c => c.GetApplicableActions(It.IsAny<ICollection<SynchronizationRule>>()))
            .Returns([originalAction]);
        _consistencyCheckerMock.Setup(c => c.CheckCanAdd(It.IsAny<AtomicAction>(), comparisonItem))
            .Returns(new AtomicActionConsistencyCheckCanAddResult(new List<ComparisonItem> { comparisonItem })
            {
                ValidationResults = [new ComparisonItemValidationResult(comparisonItem, true)]
            });
        
        _matcher.MakeMatches(comparisonItem, new List<SynchronizationRule> { rule });
        
        _repositoryMock.Verify(r => r.AddOrUpdate(It.Is<HashSet<AtomicAction>>(actions =>
            actions.All(a => a.ComparisonItem == comparisonItem && a != originalAction))), Times.Once);
    }
}