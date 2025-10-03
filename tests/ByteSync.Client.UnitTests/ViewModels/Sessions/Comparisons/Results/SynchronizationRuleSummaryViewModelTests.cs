using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Sessions;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Business.Actions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Comparisons.DescriptionBuilders;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Comparisons.Results;

[TestFixture]
public class SynchronizationRuleSummaryViewModelTests
{
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ILocalizationService> _localizationService = null!;
    private Mock<IDescriptionBuilderFactory> _descriptionBuilderFactory = null!;
    private Mock<ISynchronizationRulesService> _synchronizationRulesService = null!;
    private Mock<IDialogService> _dialogService = null!;
    private Mock<IFlyoutElementViewModelFactory> _flyoutElementViewModelFactory = null!;
    private Mock<ISynchronizationService> _synchronizationService = null!;
    
    private Subject<SessionStatus> _sessionStatusSubject = null!;
    private Subject<CultureDefinition> _cultureSubject = null!;
    private SynchronizationProcessData _processData = null!;
    
    [SetUp]
    public void SetUp()
    {
        _sessionService = new Mock<ISessionService>();
        _localizationService = new Mock<ILocalizationService>();
        _descriptionBuilderFactory = new Mock<IDescriptionBuilderFactory>();
        _synchronizationRulesService = new Mock<ISynchronizationRulesService>();
        _dialogService = new Mock<IDialogService>();
        _flyoutElementViewModelFactory = new Mock<IFlyoutElementViewModelFactory>();
        _synchronizationService = new Mock<ISynchronizationService>();
        
        _sessionStatusSubject = new Subject<SessionStatus>();
        _cultureSubject = new Subject<CultureDefinition>();
        _processData = new SynchronizationProcessData();
        
        _sessionService.SetupGet(s => s.SessionStatusObservable).Returns(_sessionStatusSubject.AsObservable());
        _sessionService.SetupGet(s => s.CurrentSessionSettings).Returns(new SessionSettings { DataType = DataTypes.FilesDirectories });
        
        _localizationService.SetupGet(l => l.CurrentCultureObservable).Returns(_cultureSubject.AsObservable());
        _localizationService.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => key);
        
        _synchronizationService.SetupGet(s => s.SynchronizationProcessData).Returns(_processData);
    }
    
    private SynchronizationRule CreateRuleWithOneConditionAndOneAction(FileSystemTypes fsType)
    {
        var rule = new SynchronizationRule(fsType, ConditionModes.All);
        
        var cond = new AtomicCondition(new DataPart("Left"), ComparisonProperty.Name, ConditionOperatorTypes.Equals, new DataPart("Right"));
        rule.Conditions.Add(cond);
        
        var action = new AtomicAction
        {
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("Target")
        };
        rule.AddAction(action);
        
        return rule;
    }
    
    private void UseRealDescriptionFactory()
    {
        var sizeUnitConverter = new Mock<ISizeUnitConverter>();
        sizeUnitConverter.Setup(c => c.GetPrintableSizeUnit(It.IsAny<SizeUnits?>())).Returns("MB");
        
        var realFactory = new DescriptionBuilderFactory(_localizationService.Object, sizeUnitConverter.Object);
        _descriptionBuilderFactory.Setup(f => f.CreateAtomicConditionDescriptionBuilder())
            .Returns(realFactory.CreateAtomicConditionDescriptionBuilder());
        _descriptionBuilderFactory.Setup(f => f.CreateAtomicActionDescriptionBuilder())
            .Returns(realFactory.CreateAtomicActionDescriptionBuilder());
        _descriptionBuilderFactory.Setup(f => f.CreateSynchronizationRuleDescriptionBuilder(It.IsAny<ISynchronizationRule>()))
            .Returns<ISynchronizationRule>(r => new SynchronizationRuleDescriptionBuilder(r, _localizationService.Object, realFactory));
    }
    
    [Test]
    public void Constructor_ShouldInitialize_Description_Icon_And_ElementType()
    {
        UseRealDescriptionFactory();
        
        var rule = CreateRuleWithOneConditionAndOneAction(FileSystemTypes.File);
        
        var vm = new SynchronizationRuleSummaryViewModel(
            rule,
            _sessionService.Object,
            _localizationService.Object,
            _descriptionBuilderFactory.Object,
            _synchronizationRulesService.Object,
            _dialogService.Object,
            _flyoutElementViewModelFactory.Object,
            _synchronizationService.Object);
        
        vm.IconName.Should().Be("RegularFile");
        vm.IsIconVisible.Should().BeTrue();
        
        vm.Mode.Should().NotBeNullOrEmpty();
        vm.Conditions.Should().NotBeNullOrEmpty();
        vm.Then.Should().NotBeNullOrEmpty();
        vm.Actions.Should().NotBeNullOrEmpty();
        
        vm.ElementType.Should().NotBeNullOrEmpty();
    }
    
    [Test]
    public async Task Commands_CanExecute_ShouldToggle_WithSynchronizationStart()
    {
        UseRealDescriptionFactory();
        var rule = CreateRuleWithOneConditionAndOneAction(FileSystemTypes.Directory);
        
        var vm = new SynchronizationRuleSummaryViewModel(
            rule,
            _sessionService.Object,
            _localizationService.Object,
            _descriptionBuilderFactory.Object,
            _synchronizationRulesService.Object,
            _dialogService.Object,
            _flyoutElementViewModelFactory.Object,
            _synchronizationService.Object);
        
        bool canExecuteRemove = false;
        bool canExecuteDuplicate = false;
        bool canExecuteEdit = false;
        
        using var sub1 = vm.RemoveCommand.CanExecute.Subscribe(v => canExecuteRemove = v);
        using var sub2 = vm.DuplicateCommand.CanExecute.Subscribe(v => canExecuteDuplicate = v);
        using var sub3 = vm.EditCommand.CanExecute.Subscribe(v => canExecuteEdit = v);
        
        _sessionStatusSubject.OnNext(SessionStatus.Preparation);
        _processData.SynchronizationStart.OnNext(null);
        await Task.Delay(50);
        
        canExecuteRemove.Should().BeTrue();
        canExecuteDuplicate.Should().BeTrue();
        canExecuteEdit.Should().BeTrue();
        
        _sessionStatusSubject.OnNext(SessionStatus.Synchronization);
        _processData.SynchronizationStart.OnNext(new Synchronization { SessionId = "s", Started = DateTimeOffset.Now, StartedBy = "u" });
        await Task.Delay(50);
        
        canExecuteRemove.Should().BeFalse();
        canExecuteDuplicate.Should().BeFalse();
        canExecuteEdit.Should().BeFalse();
    }
    
    [Test]
    public async Task DuplicateCommand_ShouldOpenFlyout_WithCloneMode()
    {
        UseRealDescriptionFactory();
        var rule = CreateRuleWithOneConditionAndOneAction(FileSystemTypes.File);
        
        var builtVm = new Mock<SynchronizationRuleGlobalViewModel>().Object;
        _flyoutElementViewModelFactory
            .Setup(f => f.BuilSynchronizationRuleGlobalViewModel(rule, true))
            .Returns(builtVm);
        
        var vm = new SynchronizationRuleSummaryViewModel(
            rule,
            _sessionService.Object,
            _localizationService.Object,
            _descriptionBuilderFactory.Object,
            _synchronizationRulesService.Object,
            _dialogService.Object,
            _flyoutElementViewModelFactory.Object,
            _synchronizationService.Object);
        
        await vm.DuplicateCommand.Execute();
        
        _flyoutElementViewModelFactory.Verify(f => f.BuilSynchronizationRuleGlobalViewModel(rule, true), Times.Once);
        _dialogService.Verify(d => d.ShowFlyout("Shell_DuplicateSynchronizationRule", false, builtVm), Times.Once);
    }
    
    [Test]
    public async Task RemoveCommand_ShouldCallServiceRemove()
    {
        UseRealDescriptionFactory();
        var rule = CreateRuleWithOneConditionAndOneAction(FileSystemTypes.File);
        
        var vm = new SynchronizationRuleSummaryViewModel(
            rule,
            _sessionService.Object,
            _localizationService.Object,
            _descriptionBuilderFactory.Object,
            _synchronizationRulesService.Object,
            _dialogService.Object,
            _flyoutElementViewModelFactory.Object,
            _synchronizationService.Object);
        
        await vm.RemoveCommand.Execute();
        
        _synchronizationRulesService.Verify(s => s.Remove(rule), Times.Once);
    }
    
    [Test]
    public async Task OnLocaleChanged_ShouldUpdateElementType_And_Descriptions()
    {
        UseRealDescriptionFactory();
        var rule = CreateRuleWithOneConditionAndOneAction(FileSystemTypes.Directory);
        
        var vm = new SynchronizationRuleSummaryViewModel(
            rule,
            _sessionService.Object,
            _localizationService.Object,
            _descriptionBuilderFactory.Object,
            _synchronizationRulesService.Object,
            _dialogService.Object,
            _flyoutElementViewModelFactory.Object,
            _synchronizationService.Object);
        
        var beforeConditions = vm.Conditions;
        var beforeThen = vm.Then;
        var beforeActions = vm.Actions;
        var beforeElementType = vm.ElementType;
        
        _localizationService.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => key + "_FR");
        
        _cultureSubject.OnNext(new CultureDefinition { Code = "fr" });
        await Task.Delay(50);
        
        // Mode is built from static Resources and may not change with the mocked localization service.
        // Ensure at least one localized description changed, and the ElementType changed.
        (vm.Conditions != beforeConditions || vm.Then != beforeThen || vm.Actions != beforeActions)
            .Should().BeTrue("localized strings should be rebuilt on culture change");
        vm.ElementType.Should().NotBe(beforeElementType);
    }
}