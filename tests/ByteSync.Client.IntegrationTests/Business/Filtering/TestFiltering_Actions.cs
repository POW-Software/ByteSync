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
        
        // Créer un mock du repository d'actions
        // _mockActionRepository = new Mock<IAtomicActionRepository>();

        _mockActionRepository = Container.Resolve<IAtomicActionRepository>();

        // // Enregistrer le mock dans le conteneur
        // Container.RegisterInstance(_mockActionRepository.Object);
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
        
        // _mockActionRepository.Setup(repo => repo.GetAtomicActions(comparisonItem))
        //     .Returns(actions);
        
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
        // _mockActionRepository.Setup(repo => repo.GetAtomicActions(comparisonItem))
        //     .Returns(new List<AtomicAction>());
        
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
        // _mockActionRepository.Setup(repo => repo.GetAtomicActions(comparisonItem))
        //     .Returns(actions);

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
            // Créer une règle de synchronisation factice
            action.SynchronizationRule = new SynchronizationRule(comparisonItem.FileSystemType, ConditionModes.All);
        }

        return action;
    }
}

// // Classe SynchronizationRule factice pour les tests
// public class SynchronizationRule
// {
//     public string RuleId { get; set; }
// }

// Extension de ComparisonItemExtensions pour les tests
// public static class ComparisonItemActionExtensions
// {
//     public static List<AtomicAction> GetAllActions(this ComparisonItem item, IAtomicActionRepository actionRepository)
//     {
//         return actionRepository.GetAtomicActions(item);
//     }
//     
//     public static List<AtomicAction> GetTargetedActions(this ComparisonItem item, IAtomicActionRepository actionRepository)
//     {
//         return actionRepository.GetAtomicActions(item)
//             .Where(a => a.IsTargeted)
//             .ToList();
//     }
//     
//     public static List<AtomicAction> GetRuleBasedActions(this ComparisonItem item, IAtomicActionRepository actionRepository)
//     {
//         return actionRepository.GetAtomicActions(item)
//             .Where(a => a.IsFromSynchronizationRule)
//             .ToList();
//     }
// }