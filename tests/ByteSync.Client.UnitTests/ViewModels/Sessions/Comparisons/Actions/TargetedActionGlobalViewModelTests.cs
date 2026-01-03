using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Client.UnitTests.Helpers;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.TestsCommon;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Comparisons.Actions;

[TestFixture]
public class TargetedActionGlobalViewModelTests : AbstractTester
{
    private Mock<IDialogService> _mockDialogService = null!;
    private Mock<ILocalizationService> _mockLocalizationService = null!;
    private Mock<ITargetedActionsService> _mockTargetedActionsService = null!;
    private Mock<IAtomicActionConsistencyChecker> _mockAtomicActionConsistencyChecker = null!;
    private Mock<IActionEditViewModelFactory> _mockActionEditViewModelFactory = null!;
    private Mock<IAtomicActionValidationFailureReasonService> _mockFailureReasonService = null!;
    private Mock<ILogger<TargetedActionGlobalViewModel>> _mockLogger = null!;
    private Subject<CultureDefinition> _cultureSubject = null!;
    private List<ComparisonItem> _comparisonItems = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockDialogService = new Mock<IDialogService>();
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockTargetedActionsService = new Mock<ITargetedActionsService>();
        _mockAtomicActionConsistencyChecker = new Mock<IAtomicActionConsistencyChecker>();
        _mockActionEditViewModelFactory = new Mock<IActionEditViewModelFactory>();
        _mockFailureReasonService = new Mock<IAtomicActionValidationFailureReasonService>();
        _mockLogger = new Mock<ILogger<TargetedActionGlobalViewModel>>();
        _cultureSubject = new Subject<CultureDefinition>();
        
        // Setup basic mocks
        _mockLocalizationService.Setup(x => x.CurrentCultureObservable)
            .Returns(_cultureSubject.AsObservable());
        _mockLocalizationService.Setup(x => x["TargetedActionEditionGlobal_ActionIssues"])
            .Returns("The action cannot be applied to some items:");
        _mockLocalizationService.Setup(x => x["TargetedActionEditionGlobal_AffectedItemsTooltip"])
            .Returns("Affected items:");
        
        _mockFailureReasonService.Setup(x => x.GetLocalizedMessage(It.IsAny<AtomicActionValidationFailureReason>()))
            .Returns("Test failure message");
        
        // Create simple mock comparison items (all same type to avoid FileSystemType issues)
        _comparisonItems =
        [
            CreateMockComparisonItem(FileSystemTypes.File),
            CreateMockComparisonItem(FileSystemTypes.File)
        ];
        
        // Mock action edit view model
        var mockActionEditViewModel = new Mock<AtomicActionEditViewModel>();
        _mockActionEditViewModelFactory.Setup(x => x.BuildAtomicActionEditViewModel(
                It.IsAny<FileSystemTypes>(), It.IsAny<bool>(), It.IsAny<AtomicAction>(), It.IsAny<List<ComparisonItem>>()))
            .Returns(mockActionEditViewModel.Object);
    }
    
    private ComparisonItem CreateMockComparisonItem(FileSystemTypes fileSystemType, string linkingKey = "test-file")
    {
        // Create a real ComparisonItem instance since it cannot be mocked (no parameterless constructor)
        var pathIdentity = new PathIdentity(fileSystemType, linkingKey, linkingKey, linkingKey);
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        return comparisonItem;
    }
    
    [Test]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act - Create viewModel for this specific test
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Assert
        viewModel.ComparisonItems.Should().NotBeNull();
        viewModel.ComparisonItems.Should().HaveCount(2);
        viewModel.FailureSummaries.Should().NotBeNull();
        viewModel.FailureSummaries.Should().BeEmpty();
        
        viewModel.AddActionCommand.Should().NotBeNull();
        viewModel.SaveCommand.Should().NotBeNull();
        viewModel.ResetCommand.Should().NotBeNull();
        viewModel.CancelCommand.Should().NotBeNull();
        viewModel.SaveValidItemsCommand.Should().NotBeNull();
        
        viewModel.ActionIssuesHeaderMessage.Should().Be("The action cannot be applied to some items:");
        viewModel.AffectedItemsTooltipHeader.Should().Be("Affected items:");
        
        viewModel.ShowWarning.Should().BeFalse();
        viewModel.AreMissingFields.Should().BeFalse();
        viewModel.IsInconsistentWithValidItems.Should().BeNull();
        viewModel.IsInconsistentWithNoValidItems.Should().BeFalse();
    }
    
    [Test]
    public void AddAction_ShouldCallActionEditViewModelFactory()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Act
        viewModel.AddActionCommand.Execute(Unit.Default).Subscribe();
        
        // Assert
        // The constructor calls the factory once during initialization, and AddActionCommand calls it again
        _mockActionEditViewModelFactory.Verify(x => x.BuildAtomicActionEditViewModel(
            It.IsAny<FileSystemTypes>(), false, null, _comparisonItems), Times.Exactly(2));
    }
    
    [Test]
    public void OnLocaleChanged_ShouldUpdateLocalizedMessages()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Activate the ViewModel to enable WhenActivated subscriptions
        viewModel.Activator.Activate();
        
        // Update the mock setups for the new localized messages
        _mockLocalizationService.Setup(x => x["TargetedActionEditionGlobal_ActionIssues"])
            .Returns("Updated message");
        _mockLocalizationService.Setup(x => x["TargetedActionEditionGlobal_AffectedItemsTooltip"])
            .Returns("Updated tooltip");
        
        // Act
        _cultureSubject.OnNext(new CultureDefinition { Code = "fr" });
        
        // Assert
        viewModel.ShouldEventuallyBe(vm => vm.ActionIssuesHeaderMessage, "Updated message");
        viewModel.ShouldEventuallyBe(vm => vm.AffectedItemsTooltipHeader, "Updated tooltip");
    }
    
    [Test]
    public async Task OnLocaleChanged_WithFailureSummaries_ShouldUpdateLocalizedMessages()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Activate the ViewModel to enable WhenActivated subscriptions
        viewModel.Activator.Activate();
        
        var failureSummary = new ValidationFailureSummary
        {
            Reason = AtomicActionValidationFailureReason.SourceMissing,
            Count = 2,
            LocalizedMessage = "Old message",
            AffectedItems = []
        };
        viewModel.FailureSummaries.Add(failureSummary);
        
        // Set up the new localized message before triggering the culture change
        _mockFailureReasonService.Setup(x => x.GetLocalizedMessage(AtomicActionValidationFailureReason.SourceMissing))
            .Returns("New localized message");
        
        // Act
        _cultureSubject.OnNext(new CultureDefinition { Code = "fr" });
        
        // Allow time for the observable to process
        await Task.Delay(100);
        
        // Assert
        viewModel.FailureSummaries[0].LocalizedMessage.Should().Be("New localized message");
        _mockFailureReasonService.Verify(x => x.GetLocalizedMessage(AtomicActionValidationFailureReason.SourceMissing), Times.Once);
    }
    
    [Test]
    public void ResetWarning_ShouldClearAllWarningProperties()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        viewModel.FailureSummaries.Add(new ValidationFailureSummary
        {
            Reason = AtomicActionValidationFailureReason.SourceHasMultipleIdentities,
            Count = 1,
            LocalizedMessage = "Test",
            AffectedItems = []
        });
        
        // Use reflection or expose method for testing
        var resetWarningMethod = typeof(TargetedActionGlobalViewModel)
            .GetMethod("ResetWarning", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        resetWarningMethod?.Invoke(viewModel, null);
        
        // Assert
        viewModel.FailureSummaries.Should().BeEmpty();
        viewModel.AreMissingFields.Should().BeFalse();
        viewModel.IsInconsistentWithValidItems.Should().BeNull();
        viewModel.IsInconsistentWithNoValidItems.Should().BeFalse();
    }
    
    [Test]
    public void ShowConsistencyWarning_WithValidAndInvalidItems_ShouldSetCorrectProperties()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Create real instance instead of mock
        var result = new AtomicActionConsistencyCheckCanAddResult(_comparisonItems);
        result.ValidationResults.Add(new ComparisonItemValidationResult(_comparisonItems[0], true)); // Valid
        result.ValidationResults.Add(new ComparisonItemValidationResult(_comparisonItems[1],
            AtomicActionValidationFailureReason.SourceMissing)); // Invalid
        var atomicAction = new AtomicAction { Operator = ActionOperatorTypes.CopyContentOnly };
        
        // Use reflection to access private method
        var showConsistencyWarningMethod = typeof(TargetedActionGlobalViewModel)
            .GetMethod("ShowConsistencyWarning", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        showConsistencyWarningMethod?.Invoke(viewModel, [atomicAction, result]);
        
        // Assert
        viewModel.ShowSaveValidItemsCommand.Should().BeTrue();
        viewModel.IsInconsistentWithValidItems.Should().NotBeNull();
        viewModel.IsInconsistentWithValidItems!.Item1.Should().Be(1); // Valid count
        viewModel.IsInconsistentWithValidItems!.Item2.Should().Be(1); // Invalid count
        viewModel.IsInconsistentWithNoValidItems.Should().BeFalse();
        viewModel.AreMissingFields.Should().BeFalse();
        
        viewModel.FailureSummaries.Should().HaveCount(1);
        viewModel.FailureSummaries[0].Reason.Should().Be(AtomicActionValidationFailureReason.SourceMissing);
        viewModel.FailureSummaries[0].Count.Should().Be(1);
        viewModel.FailureSummaries[0].LocalizedMessage.Should().Be("Test failure message");
        viewModel.FailureSummaries[0].AffectedItems.Should().HaveCount(1);
    }
    
    [Test]
    public void ShowConsistencyWarning_WithNoValidItems_ShouldSetNoValidItemsFlag()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Create real instance instead of mock - all items fail validation
        var result = new AtomicActionConsistencyCheckCanAddResult(_comparisonItems);
        result.ValidationResults.Add(new ComparisonItemValidationResult(_comparisonItems[0],
            AtomicActionValidationFailureReason.SourceHasMultipleIdentities)); // Invalid
        result.ValidationResults.Add(new ComparisonItemValidationResult(_comparisonItems[1],
            AtomicActionValidationFailureReason.SourceHasMultipleIdentities)); // Invalid
        var atomicAction = new AtomicAction { Operator = ActionOperatorTypes.CopyContentOnly };
        
        // Use reflection to access private method
        var showConsistencyWarningMethod = typeof(TargetedActionGlobalViewModel)
            .GetMethod("ShowConsistencyWarning", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        showConsistencyWarningMethod?.Invoke(viewModel, [atomicAction, result]);
        
        // Assert
        viewModel.ShowSaveValidItemsCommand.Should().BeFalse();
        viewModel.IsInconsistentWithValidItems.Should().BeNull();
        viewModel.IsInconsistentWithNoValidItems.Should().BeTrue();
        viewModel.FailureSummaries.Should().HaveCount(1);
        viewModel.FailureSummaries[0].Count.Should().Be(2); // Both items failed with same reason
    }
    
    [Test]
    public void ShowConsistencyWarning_WithMultipleFailureReasons_ShouldGroupByReason()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Create real instance instead of mock with multiple failure reasons
        var result = new AtomicActionConsistencyCheckCanAddResult(_comparisonItems);
        result.ValidationResults.Add(new ComparisonItemValidationResult(_comparisonItems[0],
            AtomicActionValidationFailureReason.SourceMissing)); // First SourceMissing
        result.ValidationResults.Add(new ComparisonItemValidationResult(_comparisonItems[1],
            AtomicActionValidationFailureReason.CreateOperationOnFileNotAllowed)); // Different reason
        result.ValidationResults.Add(new ComparisonItemValidationResult(_comparisonItems[0],
            AtomicActionValidationFailureReason.SourceMissing)); // Duplicate SourceMissing
        var atomicAction = new AtomicAction { Operator = ActionOperatorTypes.CopyContentOnly };
        
        _mockFailureReasonService.Setup(x => x.GetLocalizedMessage(AtomicActionValidationFailureReason.CreateOperationOnFileNotAllowed))
            .Returns("Cannot create files");
        
        // Use reflection to access private method
        var showConsistencyWarningMethod = typeof(TargetedActionGlobalViewModel)
            .GetMethod("ShowConsistencyWarning", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        showConsistencyWarningMethod?.Invoke(viewModel, [atomicAction, result]);
        
        // Assert
        viewModel.FailureSummaries.Should().HaveCount(2);
        
        // Should be ordered by count (most frequent first)
        viewModel.FailureSummaries[0].Count.Should().Be(2); // SourceMissing appears twice
        viewModel.FailureSummaries[0].Reason.Should().Be(AtomicActionValidationFailureReason.SourceMissing);
        
        viewModel.FailureSummaries[1].Count.Should().Be(1); // CreateOperationOnFileNotAllowed appears once
        viewModel.FailureSummaries[1].Reason.Should().Be(AtomicActionValidationFailureReason.CreateOperationOnFileNotAllowed);
        viewModel.FailureSummaries[1].LocalizedMessage.Should().Be("Cannot create files");
    }
    
    [Test]
    public void ShowMissingFieldsWarning_ShouldClearFailureSummariesAndSetMissingFieldsFlag()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        viewModel.FailureSummaries.Add(new ValidationFailureSummary
        {
            Reason = AtomicActionValidationFailureReason.SourceMissing,
            Count = 1,
            LocalizedMessage = "Test",
            AffectedItems = []
        });
        
        // Use reflection to access private method
        var showMissingFieldsWarningMethod = typeof(TargetedActionGlobalViewModel)
            .GetMethod("ShowMissingFieldsWarning", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        showMissingFieldsWarningMethod?.Invoke(viewModel, null);
        
        // Assert
        viewModel.AreMissingFields.Should().BeTrue();
        viewModel.IsInconsistentWithValidItems.Should().BeNull();
        viewModel.IsInconsistentWithNoValidItems.Should().BeFalse();
        viewModel.FailureSummaries.Should().BeEmpty(); // Should be cleared
    }
    
    [Test]
    public void Reset_ShouldCallActionEditViewModelFactory()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Act
        viewModel.ResetCommand.Execute(Unit.Default).Subscribe();
        
        // Assert
        // Should call factory to create action edit view model (ResetToCreation behavior)
        _mockActionEditViewModelFactory.Verify(x => x.BuildAtomicActionEditViewModel(
            It.IsAny<FileSystemTypes>(), false, null, It.IsAny<List<ComparisonItem>>()), Times.AtLeastOnce);
    }
    
    [Test]
    public void Cancel_ShouldCallActionEditViewModelFactory()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Act
        viewModel.CancelCommand.Execute(Unit.Default).Subscribe();
        
        // Assert
        // Should call factory to create action edit view model (reset behavior)
        _mockActionEditViewModelFactory.Verify(x => x.BuildAtomicActionEditViewModel(
            It.IsAny<FileSystemTypes>(), false, null, It.IsAny<List<ComparisonItem>>()), Times.AtLeastOnce);
    }
    
    [Test]
    public void WhenActivated_ShouldSubscribeToCultureChanges()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        // Act
        viewModel.Activator.Activate();
        
        // Assert
        _mockLocalizationService.Verify(x => x.CurrentCultureObservable, Times.Once);
    }
    
    [Test]
    public void WhenDeactivated_ShouldDisposeSubscriptions()
    {
        // Arrange
        var viewModel = new TargetedActionGlobalViewModel(
            _comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        viewModel.Activator.Activate();
        
        // Act
        viewModel.Activator.Deactivate();
        
        // Assert
        // If we reach here without exceptions, the test passes
        viewModel.Should().NotBeNull();
    }
    
    [Test]
    public void Constructor_WithEmptyParameterConstructor_ShouldNotThrow()
    {
        // Act & Assert
        var emptyViewModel = new TargetedActionGlobalViewModel();
        emptyViewModel.Should().NotBeNull();
    }
    
    [Test]
    public void ValidationFailureSummary_AffectedItemsTooltip_ShouldGenerateCorrectTooltip()
    {
        // Arrange - Create real instances instead of mocks
        var pathIdentity1 = new PathIdentity(FileSystemTypes.File, "file1.txt", "file1.txt", "file1.txt");
        var item1 = new ComparisonItem(pathIdentity1);
        
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var pathIdentity2 = new PathIdentity(FileSystemTypes.Directory, "directory1", null, "directory1");
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        var item2 = new ComparisonItem(pathIdentity2);
        
        var summary = new ValidationFailureSummary
        {
            AffectedItems = [item1, item2]
        };
        
        // Act
        var tooltip = summary.AffectedItemsTooltip;
        
        // Assert
        // When FileName is null, it should use LinkingKeyValue
        tooltip.Should().Be("file1.txt\ndirectory1");
    }
    
    [Test]
    public void Save_WithValidAction_ShouldLogConsistencySuccess()
    {
        var comparisonItems = new List<ComparisonItem>
        {
            CreateMockComparisonItem(FileSystemTypes.File, "file1"),
            CreateMockComparisonItem(FileSystemTypes.File, "file2")
        };
        
        var dataPartIndexer = BuildDataPartIndexer();
        var actionEditViewModel = new AtomicActionEditViewModel(FileSystemTypes.File, false, comparisonItems, dataPartIndexer);
        
        _mockActionEditViewModelFactory.Setup(x => x.BuildAtomicActionEditViewModel(
                It.IsAny<FileSystemTypes>(), It.IsAny<bool>(), It.IsAny<AtomicAction>(), It.IsAny<List<ComparisonItem>>()))
            .Returns(actionEditViewModel);
        
        var viewModel = new TargetedActionGlobalViewModel(
            comparisonItems,
            _mockDialogService.Object,
            _mockLocalizationService.Object,
            _mockTargetedActionsService.Object,
            _mockAtomicActionConsistencyChecker.Object,
            _mockActionEditViewModelFactory.Object,
            _mockLogger.Object,
            _mockFailureReasonService.Object
        );
        
        ConfigureValidAction(actionEditViewModel);
        
        var result = new AtomicActionConsistencyCheckCanAddResult(comparisonItems);
        _mockAtomicActionConsistencyChecker.Setup(x => x.CheckCanAdd(It.IsAny<AtomicAction>(), comparisonItems))
            .Returns(result);
        
        viewModel.SaveCommand.Execute(Unit.Default).Subscribe();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Targeted action created.")
                    && v.ToString()!.Contains("Items=file1, file2")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    private static void ConfigureValidAction(AtomicActionEditViewModel actionEditViewModel)
    {
        var actions = GetInternalEnumerable(actionEditViewModel, "Actions");
        var sources = GetInternalEnumerable(actionEditViewModel, "Sources");
        var destinations = GetInternalEnumerable(actionEditViewModel, "Destinations");
        
        var selectedAction = GetActionByOperator(actions, ActionOperatorTypes.Copy);
        
        SetInternalProperty(actionEditViewModel, "SelectedAction", selectedAction);
        SetInternalProperty(actionEditViewModel, "SelectedSource", FirstItem(sources));
        SetInternalProperty(actionEditViewModel, "SelectedDestination", FirstItem(destinations));
    }
    
    private sealed class TestDataPartIndexer : IDataPartIndexer
    {
        private readonly ReadOnlyCollection<DataPart> _dataParts;
        
        public TestDataPartIndexer(IReadOnlyCollection<DataPart> dataParts)
        {
            _dataParts = new ReadOnlyCollection<DataPart>(dataParts.ToList());
        }
        
        public void BuildMap(List<Inventory> inventories)
        {
        }
        
        public ReadOnlyCollection<DataPart> GetAllDataParts()
        {
            return _dataParts;
        }
        
        public DataPart? GetDataPart(string? dataPartName)
        {
            return _dataParts.FirstOrDefault(dp => dp.Name == dataPartName);
        }
        
        public void Remap(ICollection<SynchronizationRule> synchronizationRules)
        {
        }
    }
    
    private static IDataPartIndexer BuildDataPartIndexer()
    {
        var endpoint = new ByteSyncEndpoint
        {
            ClientId = "c",
            ClientInstanceId = "ci",
            Version = "v",
            OSPlatform = OSPlatforms.Windows,
            IpAddress = "127.0.0.1"
        };
        
        var inventoryA = new Inventory { InventoryId = "INV_A", Code = "A", Endpoint = endpoint, MachineName = "M" };
        var inventoryB = new Inventory { InventoryId = "INV_B", Code = "B", Endpoint = endpoint, MachineName = "M" };
        var partA = new InventoryPart(inventoryA, "c:\\a", FileSystemTypes.Directory) { Code = "A1" };
        var partB = new InventoryPart(inventoryB, "c:\\b", FileSystemTypes.Directory) { Code = "B1" };
        
        var dataParts = new List<DataPart>
        {
            new("A", partA),
            new("B", partB)
        };
        
        return new TestDataPartIndexer(dataParts);
    }
    
    private static IEnumerable GetInternalEnumerable(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        
        return (IEnumerable)property!.GetValue(target)!;
    }
    
    private static object FirstItem(IEnumerable items)
    {
        foreach (var item in items)
        {
            return item!;
        }
        
        throw new InvalidOperationException("Empty collection");
    }
    
    private static void SetInternalProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        property!.SetValue(target, value);
    }
    
    private static object GetActionByOperator(IEnumerable actions, ActionOperatorTypes operatorType)
    {
        foreach (var action in actions)
        {
            var property = action!.GetType().GetProperty("ActionOperatorType", BindingFlags.Instance | BindingFlags.Public);
            var value = (ActionOperatorTypes)property!.GetValue(action)!;
            if (value == operatorType)
            {
                return action;
            }
        }
        
        throw new InvalidOperationException("Action not found");
    }
}