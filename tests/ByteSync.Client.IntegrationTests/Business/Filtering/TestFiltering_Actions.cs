using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Actions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Actions : BaseTestFiltering
{
    private IAtomicActionRepository _mockActionRepository;
    
    [SetUp]
    public void Setup()
    {
        SetupBase();
        
        _mockActionRepository = Container.Resolve<IAtomicActionRepository>();
    }
    
    [Test]
    public void TestFiltering_Actions_CountGreaterThanZero()
    {
        // Arrange
        var comparisonItem = CreateBasicComparisonItem();
        
        var actions = new List<AtomicAction>
        {
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.SynchronizeContentOnly, false),
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.Delete, true)
        };
        
        _mockActionRepository.AddOrUpdate(actions);
        
        var filterText = "actions>0";
        
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestFiltering_Actions_CountEqualsZero()
    {
        // Arrange
        var comparisonItem = CreateBasicComparisonItem();
        
        _mockActionRepository.AddOrUpdate(new List<AtomicAction>());
        
        var filterText = "actions==0";
        
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestFiltering_TargetedActions()
    {
        // Arrange
        var comparisonItem = CreateBasicComparisonItem();

        var actions = new List<AtomicAction>
        {
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.SynchronizeContentOnly, false),
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.Delete, true)
        };

        _mockActionRepository.AddOrUpdate(actions);

        var filterText = "actions.targeted>0";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }


    [Test]
    public void TestFiltering_RuleActions()
    {
        // Arrange
        var comparisonItem = CreateBasicComparisonItem();

        var actions = new List<AtomicAction>
        {
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.SynchronizeContentOnly, false),
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.Delete, true)
        };

        _mockActionRepository.AddOrUpdate(actions);

        var filterText = "actions.rules>0";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestFiltering_ActionsByType_Delete()
    {
        // Arrange
        var comparisonItem = CreateBasicComparisonItem();

        var actions = new List<AtomicAction>
        {
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.SynchronizeContentOnly, false),
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.Delete, true)
        };

        _mockActionRepository.AddOrUpdate(actions);

        var filterText = "actions.delete>0";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestFiltering_TargetedActionsByType_Delete()
    {
        // Arrange
        var comparisonItem = CreateBasicComparisonItem();

        var actions = new List<AtomicAction>
        {
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.SynchronizeContentOnly, false),
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.Delete, true)
        };

        _mockActionRepository.AddOrUpdate(actions);

        var filterText = "actions.targeted.delete>0";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }


    [Test]
    public void TestFiltering_RuleActionsByType_SynchronizeContent()
    {
        // Arrange
        var comparisonItem = CreateBasicComparisonItem();

        var actions = new List<AtomicAction>
        {
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.SynchronizeContentOnly, false),
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.Delete, true)
        };

        _mockActionRepository.AddOrUpdate(actions);

        var filterText = "actions.rules.synchronizecontent>0";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestFiltering_NoSuchActions()
    {
        // Arrange
        var comparisonItem = CreateBasicComparisonItem();

        var actions = new List<AtomicAction>
        {
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.SynchronizeContentOnly, false),
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.Delete, true)
        };

        _mockActionRepository.AddOrUpdate(actions);

        var filterText = "actions.targeted.create==0";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestFiltering_ComplexCondition()
    {
        // Arrange
        var comparisonItem = CreateBasicComparisonItem();

        var actions = new List<AtomicAction>
        {
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.SynchronizeContentOnly, false),
            CreateAtomicAction(comparisonItem, ActionOperatorTypes.Delete, true)
        };

        _mockActionRepository.AddOrUpdate(actions);

        var filterText = "actions.delete>0 AND actions.create==0";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }


    private AtomicAction CreateAtomicAction(ComparisonItem comparisonItem, ActionOperatorTypes operatorType, bool isTargeted)
    {
        var action = new AtomicAction($"AAID_{Guid.NewGuid()}", comparisonItem);
        action.Operator = operatorType;

        if (!isTargeted)
        {
            action.SynchronizationRule = new SynchronizationRule(comparisonItem.FileSystemType, ConditionModes.All);
        }

        return action;
    }
}