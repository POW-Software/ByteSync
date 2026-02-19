using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Business.Sessions;
using ByteSync.Business.Themes;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Sessions.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Inventories;

[TestFixture]
public class InventoryGlobalStatusViewModelTests
{
    private InventoryProcessData _processData = null!;
    private Subject<InventoryStatistics?> _statsSubject = null!;
    private Subject<SessionStatus> _sessionStatusSubject = null!;
    private Mock<IInventoryService> _inventoryService = null!;
    private Mock<IInventoryStatisticsService> _statsService = null!;
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ITimeTrackingCache> _timeTrackingCache = null!;
    private Mock<IDialogService> _dialogService = null!;
    private Mock<IThemeService> _themeService = null!;
    private Mock<ILogger<InventoryGlobalStatusViewModel>> _logger = null!;
    
    [SetUp]
    public void Setup()
    {
        _processData = new InventoryProcessData();
        _statsSubject = new Subject<InventoryStatistics?>();
        
        _inventoryService = new Mock<IInventoryService>();
        _inventoryService.SetupGet(x => x.InventoryProcessData).Returns(_processData);
        
        _statsService = new Mock<IInventoryStatisticsService>();
        _statsService.SetupGet(x => x.Statistics).Returns(_statsSubject.AsObservable());
        
        _themeService = new Mock<IThemeService>();
        _themeService.SetupGet(t => t.SelectedTheme)
            .Returns(Observable.Never<Theme>());
        
        _sessionService = new Mock<ISessionService>();
        _sessionService.SetupGet(x => x.SessionId).Returns("test-session");
        _sessionStatusSubject = new Subject<SessionStatus>();
        _sessionService.SetupGet(x => x.SessionStatusObservable).Returns(_sessionStatusSubject.AsObservable());
        
        var mockComputer = new Mock<ITimeTrackingComputer>();
        mockComputer.SetupGet(x => x.RemainingTime).Returns(Observable.Empty<TimeTrack>());
        
        _timeTrackingCache = new Mock<ITimeTrackingCache>();
        _timeTrackingCache
            .Setup(x => x.GetTimeTrackingComputer(It.IsAny<string>(), It.IsAny<TimeTrackingComputerType>()))
            .ReturnsAsync(mockComputer.Object);
        
        _dialogService = new Mock<IDialogService>();
        _logger = new Mock<ILogger<InventoryGlobalStatusViewModel>>();
    }
    
    private InventoryGlobalStatusViewModel CreateVm()
    {
        var vm = new InventoryGlobalStatusViewModel(
            _inventoryService.Object,
            _sessionService.Object,
            _dialogService.Object,
            _themeService.Object,
            _statsService.Object,
            _logger.Object);
        vm.Activator.Activate();
        
        return vm;
    }
    
    [Test]
    public void NonSuccessTerminal_ShowsErrorIcon_AndNoSpinner()
    {
        var vm = CreateVm();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Error);
        
        vm.GlobalMainIcon.Should().Be("SolidXCircle");
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void Cancelled_ShowsErrorIcon_AndNoSpinner()
    {
        var vm = CreateVm();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Cancelled);
        
        vm.GlobalMainIcon.Should().Be("SolidXCircle");
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void NotLaunched_ShowsErrorIcon_AndNoSpinner()
    {
        var vm = CreateVm();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.NotLaunched);
        
        vm.GlobalMainIcon.Should().Be("SolidXCircle");
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void SuccessThenStatsWithErrors_ShowsRegularError_AndStopsSpinner()
    {
        var vm = CreateVm();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        vm.IsInventoryInProgress.Should().BeTrue("waiting for final stats");
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 3, AnalyzeSuccess = 0, TotalAnalyzed = 0 });
        
        vm.HasErrors.Should().BeTrue();
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void SuccessThenStatsWithoutErrors_ShowsCheck_AndStopsSpinner()
    {
        var vm = CreateVm();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        vm.IsInventoryInProgress.Should().BeTrue();
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 0, AnalyzeSuccess = 10, TotalAnalyzed = 10 });
        
        vm.HasErrors.Should().BeFalse();
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void StatsBeforeSuccessWithErrors_RendersFinalImmediatelyOnSuccess()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 2, AnalyzeSuccess = 0, TotalAnalyzed = 2 });
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.HasErrors.Should().BeTrue();
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void StatsBeforeSuccessWithoutErrors_RendersCheckImmediatelyOnSuccess()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 0, AnalyzeSuccess = 5, TotalAnalyzed = 5 });
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.GlobalMainIcon.Should().Be("SolidCheckCircle");
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void StatsNullBeforeSuccessThenNonNullAfter_GatesUntilNonNull()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(null);
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.IsInventoryInProgress.Should().BeTrue("waiting for first non-null statistics");
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 0, AnalyzeSuccess = 3, TotalAnalyzed = 3 });
        
        vm.GlobalMainIcon.Should().Be("SolidCheckCircle");
        vm.IsInventoryInProgress.Should().BeFalse();
    }
    
    [Test]
    public void SuccessThenCancelled_BeforeStats_StopsSpinnerAndRendersNonSuccess()
    {
        var vm = CreateVm();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.IsInventoryInProgress.Should().BeTrue();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Cancelled);
        
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainIcon.Should().Be("SolidXCircle");
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void HasErrors_ComputedFromGlobalAnalyzeErrors()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 4, AnalyzeSuccess = 0, TotalAnalyzed = 4 });
        vm.HasErrors.Should().BeTrue();
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 0, AnalyzeSuccess = 10, TotalAnalyzed = 10 });
        vm.HasErrors.Should().BeFalse();
    }
    
    [Test]
    public void SessionPreparation_ResetsStatisticsAndVisuals()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 1, AnalyzeSuccess = 2, TotalAnalyzed = 3 });
        vm.GlobalAnalyzeErrors.Should().Be(1);
        
        vm.GlobalMainIcon = "SolidCheckCircle";
        vm.GlobalMainStatusText = "dummy";
        vm.GlobalMainIconBrush = null;
        
        _sessionStatusSubject.OnNext(SessionStatus.Preparation);
        
        vm.GlobalTotalAnalyzed.Should().Be(null);
        vm.GlobalAnalyzeSuccess.Should().Be(null);
        vm.GlobalAnalyzeErrors.Should().Be(null);
        vm.GlobalIdentificationErrors.Should().Be(null);
        vm.GlobalMainIcon.Should().Be("None");
        vm.GlobalMainStatusText.Should().BeEmpty();
    }
    
    [Test]
    public void HasIdentificationErrors_ComputedFromStats()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 0, AnalyzeSuccess = 1, TotalAnalyzed = 1, IdentificationErrors = 2 });
        vm.HasIdentificationErrors.Should().BeTrue();
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 0, AnalyzeSuccess = 1, TotalAnalyzed = 1, IdentificationErrors = 0 });
        vm.HasIdentificationErrors.Should().BeFalse();
    }
    
    [Test]
    public void SuccessWithIdentificationErrors_RendersErrorState()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(new InventoryStatistics { AnalyzeErrors = 0, AnalyzeSuccess = 1, TotalAnalyzed = 1, IdentificationErrors = 1 });
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.HasIdentificationErrors.Should().BeTrue();
        vm.GlobalMainIcon.Should().Be("RegularError");
    }

    [Test]
    public void ShowGlobalSkippedEntries_ComputedFromStats()
    {
        var vm = CreateVm();

        _statsSubject.OnNext(new InventoryStatistics { TotalSkippedEntries = 4 });
        vm.ShowGlobalSkippedEntries.Should().BeTrue();
        vm.GlobalSkippedEntries.Should().Be(4);

        _statsSubject.OnNext(new InventoryStatistics { TotalSkippedEntries = 0 });
        vm.ShowGlobalSkippedEntries.Should().BeFalse();
        vm.GlobalSkippedEntries.Should().Be(0);
    }
    
    [Test]
    public async Task AbortCommand_UserConfirms_RequestsAbort()
    {
        var messageBoxViewModel = new MessageBoxViewModel();
        _dialogService.Setup(x => x.CreateMessageBoxViewModel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]?>()))
            .Returns(messageBoxViewModel);
        _dialogService.Setup(x => x.ShowMessageBoxAsync(messageBoxViewModel))
            .ReturnsAsync(MessageBoxResult.Yes);
        _inventoryService.Setup(x => x.AbortInventory()).Returns(Task.CompletedTask).Verifiable();
        
        var vm = CreateVm();
        
        await vm.AbortInventoryCommand.Execute().ToTask();
        
        messageBoxViewModel.ShowYesNo.Should().BeTrue();
        _dialogService.Verify(x => x.ShowMessageBoxAsync(messageBoxViewModel), Times.Once);
        _inventoryService.Verify(x => x.AbortInventory(), Times.Once);
    }
    
    [Test]
    public async Task AbortCommand_UserDeclines_DoesNotAbort()
    {
        var messageBoxViewModel = new MessageBoxViewModel();
        _dialogService.Setup(x => x.CreateMessageBoxViewModel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]?>()))
            .Returns(messageBoxViewModel);
        _dialogService.Setup(x => x.ShowMessageBoxAsync(messageBoxViewModel))
            .ReturnsAsync(MessageBoxResult.No);
        _inventoryService.Setup(x => x.AbortInventory()).Returns(Task.CompletedTask).Verifiable();
        
        var vm = CreateVm();
        
        await vm.AbortInventoryCommand.Execute().ToTask();
        
        _inventoryService.Verify(x => x.AbortInventory(), Times.Never);
    }
}
