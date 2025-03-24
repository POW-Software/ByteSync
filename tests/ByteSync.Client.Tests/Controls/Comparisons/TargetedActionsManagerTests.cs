using Moq;
using NUnit.Framework;
using ByteSync.Services.Comparisons;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Tests.Controls.Comparisons;

[TestFixture]
public class TargetedActionsManagerTests
{
    private Mock<ISynchronizationRuleMatcher> _synchronizationRuleMatcherMock;
    private Mock<IAtomicActionRepository> _atomicActionRepositoryMock;
    private Mock<ISynchronizationRuleRepository> _synchronizationRuleRepositoryMock;
    private TargetedActionsManager _manager;

    [SetUp]
    public void SetUp()
    {
        _synchronizationRuleMatcherMock = new Mock<ISynchronizationRuleMatcher>();
        _atomicActionRepositoryMock = new Mock<IAtomicActionRepository>();
        _synchronizationRuleRepositoryMock = new Mock<ISynchronizationRuleRepository>();

        _manager = new TargetedActionsManager(
            _synchronizationRuleMatcherMock.Object,
            _atomicActionRepositoryMock.Object,
            _synchronizationRuleRepositoryMock.Object
        );
    }

    [Test]
    public void AddTargetedAction_ShouldAddActionToRepository()
    {
        _atomicActionRepositoryMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>()))
            .Returns(new List<AtomicAction>());
        
        var atomicAction = new AtomicAction();
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "linkingKey", "name", "linkingData");
        
        var comparisonItem = new ComparisonItem(pathIdentity);

        _manager.AddTargetedAction(atomicAction, comparisonItem);

        _atomicActionRepositoryMock.Verify(r => r.Remove(It.IsAny<AtomicAction>()), Times.Never);
        _atomicActionRepositoryMock.Verify(r => r.AddOrUpdate(It.IsAny<AtomicAction>()), Times.Once);
    }

    [Test]
    public void AddTargetedAction_MultipleComparisonItems_ShouldAddActions()
    {
        var comparisonItems = new List<ComparisonItem>
        {
            new ComparisonItem(new PathIdentity(FileSystemTypes.File, "key1", "name1", "data1")),
            new ComparisonItem(new PathIdentity(FileSystemTypes.Directory, "key2", "name2", "data2"))
        };
        _atomicActionRepositoryMock
            .Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>()))
            .Returns(new List<AtomicAction>());
        var atomicAction = new AtomicAction { Operator = ActionOperatorTypes.Create };

        _manager.AddTargetedAction(atomicAction, comparisonItems);

        _atomicActionRepositoryMock.Verify(r => r.AddOrUpdate(It.IsAny<AtomicAction>()),
            Times.Exactly(comparisonItems.Count));
    }
    
    [Test]
    public void AddTargetedAction_DoNothing_ShouldRemoveExistingAndAddDoNothing()
    {
        // Arrange
        var existingActions = new List<AtomicAction>
        {
            new AtomicAction { Operator = ActionOperatorTypes.Create, SynchronizationRule = null }
        };
        _atomicActionRepositoryMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>()))
            .Returns(existingActions);

        var doNothingAction = new AtomicAction { Operator = ActionOperatorTypes.DoNothing };

        // Act
        _manager.AddTargetedAction(doNothingAction, new ComparisonItem(new PathIdentity(FileSystemTypes.File, "key", "name", "data")));

        // Assert
        _atomicActionRepositoryMock.Verify(r => r.Remove(existingActions), Times.Once);
        _atomicActionRepositoryMock.Verify(r => r.AddOrUpdate(It.Is<AtomicAction>(a => a.Operator == ActionOperatorTypes.DoNothing)), Times.Once);
    }

    [Test]
    public void AddTargetedAction_WhenExistingIsDoNothing_ShouldRemoveExistingAndAddDoNothing()
    {
        // Arrange
        var doNothingExisting = new List<AtomicAction>
        {
            new AtomicAction { Operator = ActionOperatorTypes.DoNothing, SynchronizationRule = null }
        };
        _atomicActionRepositoryMock.Setup(r => r.GetAtomicActions(It.IsAny<ComparisonItem>()))
            .Returns(doNothingExisting);

        var newDoNothingAction = new AtomicAction { Operator = ActionOperatorTypes.DoNothing };

        // Act
        _manager.AddTargetedAction(newDoNothingAction, new ComparisonItem(new PathIdentity(FileSystemTypes.File, "key", "name", "data")));

        // Assert
        _atomicActionRepositoryMock.Verify(r => r.Remove(doNothingExisting), Times.Once);
        _atomicActionRepositoryMock.Verify(r => r.AddOrUpdate(It.Is<AtomicAction>(a => a.Operator == ActionOperatorTypes.DoNothing)), Times.Once);
    }
}
