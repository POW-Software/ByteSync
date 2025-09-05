using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Sessions.Comparisons.Actions;

[TestFixture]
public class SynchronizationRuleGlobalViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ILocalizationService> _localizationService = null!;
    private Mock<IActionEditViewModelFactory> _actionEditViewModelFactory = null!;
    private Mock<ISynchronizationRulesService> _synchronizationRulesService = null!;

    private Subject<CultureDefinition> _cultureSubject = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        _sessionService = new Mock<ISessionService>(MockBehavior.Strict);
        _localizationService = new Mock<ILocalizationService>(MockBehavior.Strict);
        _actionEditViewModelFactory = new Mock<IActionEditViewModelFactory>(MockBehavior.Strict);
        _synchronizationRulesService = new Mock<ISynchronizationRulesService>(MockBehavior.Strict);

        _cultureSubject = new Subject<CultureDefinition>();

        _localizationService.SetupGet(l => l.CurrentCultureObservable)
            .Returns(_cultureSubject.AsObservable());
    }

    private void SetupSession(DataTypes dataType)
    {
        _sessionService.SetupGet(s => s.CurrentSessionSettings).Returns(new SessionSettings { DataType = dataType });
    }

    // Test doubles to surface protected helpers
    private class TestAtomicActionEditViewModel : AtomicActionEditViewModel
    {
        public void TriggerRemove() => typeof(AtomicActionEditViewModel)
            .GetMethod("Remove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(this, null);
    }

    private class TestAtomicConditionEditViewModel : AtomicConditionEditViewModel
    {
        public void TriggerRemove() => typeof(AtomicConditionEditViewModel)
            .GetMethod("Remove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
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
            .Setup(f => f.BuildAtomicActionEditViewModel(It.IsAny<FileSystemTypes>(), It.IsAny<bool>(), It.IsAny<AtomicAction?>(), It.IsAny<List<ByteSync.Models.Comparisons.Result.ComparisonItem>?>()))
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
        _actionEditViewModelFactory.Verify(f => f.BuildAtomicActionEditViewModel(It.IsAny<FileSystemTypes>(), true, null, null), Times.AtLeastOnce);
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
        _actionEditViewModelFactory.Verify(f => f.BuildAtomicActionEditViewModel(It.IsAny<FileSystemTypes>(), true, null, null), Times.AtLeastOnce);
    }

    [Test]
    public void Reset_WithBaseRule_ShouldRebuildFromRule()
    {
        // Arrange
        SetupSession(DataTypes.FilesDirectories);
        SetupFactoryReturningNewAtomicVMs(out _, out _);

        var baseRule = new SynchronizationRule(FileSystemTypes.Directory, ConditionModes.Any);
        baseRule.Conditions.Add(new AtomicCondition(new DataPart("L"), ComparisonProperty.Name, ConditionOperatorTypes.Equals, new DataPart("R")));
        baseRule.AddAction(new AtomicAction
        {
            Operator = ByteSync.Common.Business.Actions.ActionOperatorTypes.Create,
            Destination = new DataPart("Dest")
        });

        // Act
        var vm = new SynchronizationRuleGlobalViewModel(
            _dialogService.Object,
            _sessionService.Object,
            _localizationService.Object,
            _actionEditViewModelFactory.Object,
            _synchronizationRulesService.Object,
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
            null,
            false);

        // Act
        vm.Activator.Activate();

        // Assert
        _localizationService.VerifyGet(l => l.CurrentCultureObservable, Times.Once);
    }
}
