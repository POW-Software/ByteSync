using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Autofac;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.Services;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Comparisons.Actions;

[TestFixture]
public class SynchronizationRuleGlobalViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ILocalizationService> _localizationService = null!;
    private Mock<IActionEditViewModelFactory> _actionEditViewModelFactory = null!;
    private Mock<ISynchronizationRulesService> _synchronizationRulesService = null!;
    private Mock<ILogger<SynchronizationRuleGlobalViewModel>> _logger = null!;
    
    private Subject<CultureDefinition> _cultureSubject = null!;
    
    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        _sessionService = new Mock<ISessionService>(MockBehavior.Strict);
        _localizationService = new Mock<ILocalizationService>(MockBehavior.Strict);
        _actionEditViewModelFactory = new Mock<IActionEditViewModelFactory>(MockBehavior.Strict);
        _synchronizationRulesService = new Mock<ISynchronizationRulesService>(MockBehavior.Strict);
        _logger = new Mock<ILogger<SynchronizationRuleGlobalViewModel>>();
        
        _cultureSubject = new Subject<CultureDefinition>();
        
        _localizationService.SetupGet(l => l.CurrentCultureObservable)
            .Returns(_cultureSubject.AsObservable());
    }
    
    private void SetupSession(DataTypes dataType)
    {
        _sessionService.SetupGet(s => s.CurrentSessionSettings).Returns(new SessionSettings { DataType = dataType });
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
    
    private static TestDataPartIndexer BuildDataPartIndexer()
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
    
    private static AtomicConditionEditViewModel BuildValidConditionViewModel(IDataPartIndexer dataPartIndexer)
    {
        var conditionVm = new AtomicConditionEditViewModel(FileSystemTypes.File, dataPartIndexer);
        
        var sourceOrProperties = (IEnumerable)GetInternalProperty(conditionVm, "SourceOrProperties");
        var comparisonOperators = (IEnumerable)GetInternalProperty(conditionVm, "ComparisonOperators");
        var destinations = (IEnumerable)GetInternalProperty(conditionVm, "ConditionDestinations");
        
        SetInternalProperty(conditionVm, "SelectedSourceOrProperty", FirstWhereBoolProperty(sourceOrProperties, "IsDataPart", true));
        SetInternalProperty(conditionVm, "SelectedComparisonOperator", FirstItem(comparisonOperators));
        SetInternalProperty(conditionVm, "SelectedDestination", FirstWhereBoolProperty(destinations, "IsVirtual", false));
        
        return conditionVm;
    }
    
    private static AtomicActionEditViewModel BuildValidActionViewModel(IDataPartIndexer dataPartIndexer)
    {
        var actionVm = new AtomicActionEditViewModel(FileSystemTypes.File, true, null, dataPartIndexer);
        
        var actions = (IEnumerable)GetInternalProperty(actionVm, "Actions");
        var sources = (IEnumerable)GetInternalProperty(actionVm, "Sources");
        
        SetInternalProperty(actionVm, "SelectedAction", FirstItem(actions));
        SetInternalProperty(actionVm, "SelectedSource", FirstItem(sources));
        
        var destinations = (IEnumerable)GetInternalProperty(actionVm, "Destinations");
        SetInternalProperty(actionVm, "SelectedDestination", FirstItem(destinations));
        
        return actionVm;
    }
    
    private static object GetInternalProperty(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        
        return property!.GetValue(target)!;
    }
    
    private static void SetInternalProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        property!.SetValue(target, value);
    }
    
    private static object FirstItem(IEnumerable items)
    {
        foreach (var item in items)
        {
            return item!;
        }
        
        throw new InvalidOperationException("Empty collection");
    }
    
    private static object FirstWhereBoolProperty(IEnumerable items, string propertyName, bool expectedValue)
    {
        foreach (var item in items)
        {
            var property = item!.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(bool)
                                 && (bool)property.GetValue(item)! == expectedValue)
            {
                return item;
            }
        }
        
        throw new InvalidOperationException($"No item found with {propertyName}={expectedValue}");
    }
    
    // Test doubles to surface protected helpers
    private class TestAtomicActionEditViewModel : AtomicActionEditViewModel
    {
        public void TriggerRemove() => typeof(AtomicActionEditViewModel)
            .GetMethod("Remove", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(this, null);
    }
    
    private class TestAtomicConditionEditViewModel : AtomicConditionEditViewModel
    {
        public void TriggerRemove() => typeof(AtomicConditionEditViewModel)
            .GetMethod("Remove", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(this, null);
    }
    
    private void SetupFactoryReturningNewAtomicVMs(out TestAtomicConditionEditViewModel condVm, out TestAtomicActionEditViewModel actVm)
    {
        condVm = new TestAtomicConditionEditViewModel();
        actVm = new TestAtomicActionEditViewModel();
        
        _actionEditViewModelFactory
            .Setup(f => f.BuildAtomicConditionEditViewModel(It.IsAny<FileSystemTypes>(), It.IsAny<AtomicCondition?>()))
            .Returns(condVm);
        
        _actionEditViewModelFactory
            .Setup(f => f.BuildAtomicActionEditViewModel(It.IsAny<FileSystemTypes>(), It.IsAny<bool>(), It.IsAny<AtomicAction?>(),
                It.IsAny<List<ComparisonItem>?>()))
            .Returns(actVm);
    }
    
    [Test]
    public void Constructor_WithFilesDirectories_ShouldInitDefaults_AndAddOneOfEach()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);
        
        // Act
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        // Assert
        vm.ShowFileSystemTypeSelection.Should().BeTrue();
        vm.FileSystemTypes.Should().HaveCount(2);
        vm.SelectedFileSystemType.IsFile.Should().BeTrue();
        
        vm.ConditionModes.Should().HaveCount(2);
        vm.SelectedConditionMode.IsAll.Should().BeTrue();
        vm.TextAfterConditionModesComboBox.Should().NotBeNullOrEmpty();
        
        vm.Conditions.Should().HaveCount(1);
        vm.Actions.Should().HaveCount(1);
        
        _actionEditViewModelFactory.Verify(f => f.BuildAtomicConditionEditViewModel(It.IsAny<FileSystemTypes>(), null), Times.AtLeastOnce);
        _actionEditViewModelFactory.Verify(f => f.BuildAtomicActionEditViewModel(It.IsAny<FileSystemTypes>(), true, null, null),
            Times.AtLeastOnce);
    }
    
    [Test]
    public void Constructor_WithDirectories_ShouldSelectDirectory_AndHideTypeSelection()
    {
        // Arrange
        SetupSession(DataTypes.Directories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);
        
        // Act
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        // Assert
        vm.ShowFileSystemTypeSelection.Should().BeFalse();
        vm.SelectedFileSystemType.IsDirectory.Should().BeTrue();
    }
    
    [Test]
    public void ChangingConditionMode_ShouldUpdateTrailingText()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);
        
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        // Act
        var before = vm.TextAfterConditionModesComboBox;
        vm.SelectedConditionMode = vm.ConditionModes.First(cm => cm.IsAny);
        
        // Assert
        vm.TextAfterConditionModesComboBox.Should().NotBeNullOrEmpty();
        vm.TextAfterConditionModesComboBox.Should().NotBe(before);
    }
    
    [Test]
    public void AddCommands_ShouldCreateNewAtomicVMs()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);
        
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        // Act
        vm.AddConditionCommand.Execute().Subscribe();
        vm.AddActionCommand.Execute().Subscribe();
        
        // Assert
        vm.Conditions.Should().HaveCount(2);
        vm.Actions.Should().HaveCount(2);
    }
    
    [Test]
    public void RemoveRequested_ShouldRemoveItems_FromCollections()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out var condVm, out var actVm);
        
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        vm.Conditions.Should().HaveCount(1);
        vm.Actions.Should().HaveCount(1);
        
        // Act - trigger remove on the test doubles (invokes protected Remove())
        actVm.TriggerRemove();
        condVm.TriggerRemove();
        
        // Assert
        vm.Actions.Should().BeEmpty();
        vm.Conditions.Should().BeEmpty();
    }
    
    [Test]
    public void Save_WithMissingFields_ShouldShowWarning_AndNotPersist()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);
        
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        // Act
        vm.SaveCommand.Execute().Subscribe();
        
        // Assert
        vm.ShowWarning.Should().BeTrue();
        vm.SaveWarning.Should().NotBeNullOrEmpty();
        _synchronizationRulesService.Verify(s => s.AddOrUpdateSynchronizationRule(It.IsAny<SynchronizationRule>()), Times.Never);
        _dialogService.Verify(d => d.CloseFlyout(), Times.Never);
    }
    
    [Test]
    public void Save_WithValidFields_ShouldPersistAndLogSuccess()
    {
        SetupSession(DataTypes.FilesDirectories);
        _localizationService.Setup(l => l[It.IsAny<string>()]).Returns("unit");
        
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_localizationService.Object).As<ILocalizationService>();
        ContainerProvider.Container = containerBuilder.Build();
        
        var dataPartIndexer = BuildDataPartIndexer();
        var conditionVm = BuildValidConditionViewModel(dataPartIndexer);
        var actionVm = BuildValidActionViewModel(dataPartIndexer);
        
        _actionEditViewModelFactory
            .Setup(f => f.BuildAtomicConditionEditViewModel(It.IsAny<FileSystemTypes>(), It.IsAny<AtomicCondition?>()))
            .Returns(conditionVm);
        
        _actionEditViewModelFactory
            .Setup(f => f.BuildAtomicActionEditViewModel(It.IsAny<FileSystemTypes>(), It.IsAny<bool>(), It.IsAny<AtomicAction?>(),
                It.IsAny<List<ComparisonItem>?>()))
            .Returns(actionVm);
        
        _synchronizationRulesService
            .Setup(s => s.AddOrUpdateSynchronizationRule(It.IsAny<SynchronizationRule>()));
        
        _dialogService.Setup(d => d.CloseFlyout());
        
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        vm.SaveCommand.Execute().Subscribe();
        
        _synchronizationRulesService.Verify(s => s.AddOrUpdateSynchronizationRule(It.IsAny<SynchronizationRule>()), Times.Once);
        _dialogService.Verify(d => d.CloseFlyout(), Times.Once);
        _logger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Synchronization rule saved.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public void SelectedFileSystemType_Change_ShouldResetCollections()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);
        
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        vm.Conditions.Should().HaveCount(1);
        vm.Actions.Should().HaveCount(1);
        
        // Prepare verification that factory is called again during reset
        _actionEditViewModelFactory.Invocations.Clear();
        
        // Act - switch file system type to directory
        vm.SelectedFileSystemType = vm.FileSystemTypes.First(f => f.IsDirectory);
        
        // Assert - a reset occurred; there should still be exactly one of each, rebuilt
        vm.Conditions.Should().HaveCount(1);
        vm.Actions.Should().HaveCount(1);
        _actionEditViewModelFactory.Verify(f => f.BuildAtomicConditionEditViewModel(It.IsAny<FileSystemTypes>(), null), Times.AtLeastOnce);
        _actionEditViewModelFactory.Verify(f => f.BuildAtomicActionEditViewModel(It.IsAny<FileSystemTypes>(), true, null, null),
            Times.AtLeastOnce);
    }
    
    [Test]
    public void Reset_WithBaseRule_ShouldRebuildFromRule()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);
        
        var baseRule = new SynchronizationRule(FileSystemTypes.Directory, ConditionModes.Any);
        baseRule.Conditions.Add(new AtomicCondition(new DataPart("L"), ComparisonProperty.Name, ConditionOperatorTypes.Equals,
            new DataPart("R")));
        baseRule.AddAction(new AtomicAction
        {
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("Dest")
        });
        
        // Act
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            baseRule,
            false);
        
        // Assert
        vm.SelectedFileSystemType.IsDirectory.Should().BeTrue();
        vm.SelectedConditionMode.IsAny.Should().BeTrue();
        vm.Conditions.Should().HaveCount(1);
        vm.Actions.Should().HaveCount(1);
    }
    
    [Test]
    public void Cancel_ShouldCloseFlyout_AndReset()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);
        
        _dialogService.Setup(d => d.CloseFlyout());
        
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        // Add an extra item so reset effect is visible
        vm.AddActionCommand.Execute().Subscribe();
        
        // Act
        vm.CancelCommand.Execute().Subscribe();
        
        // Assert
        _dialogService.Verify(d => d.CloseFlyout(), Times.Once);
        vm.Actions.Should().HaveCount(1);
        vm.Conditions.Should().HaveCount(1);
    }
    
    [Test]
    public void WhenActivated_ShouldSubscribeToCultureChanges()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);
        
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
            _logger.Object,
            null,
            false);
        
        // Act
        vm.Activator.Activate();
        
        // Assert
        _localizationService.VerifyGet(l => l.CurrentCultureObservable, Times.Once);
    }
}