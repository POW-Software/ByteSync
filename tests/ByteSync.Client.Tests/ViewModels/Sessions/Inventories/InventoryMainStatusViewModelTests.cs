using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Sessions.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Sessions.Inventories;

[TestFixture]
public class InventoryMainStatusViewModelTests
{
    private InventoryProcessData _processData = null!;
    private Subject<InventoryStatistics?> _statsSubject = null!;
    private Subject<SessionStatus> _sessionStatusSubject = null!;
    private Mock<IInventoryService> _inventoryService = null!;
    private Mock<IInventoryStatisticsService> _statsService = null!;
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ITimeTrackingCache> _timeTrackingCache = null!;
    private Mock<IDialogService> _dialogService = null!;
    private Mock<ILogger<InventoryMainStatusViewModel>> _logger = null!;
    
    [SetUp]
    public void Setup()
    {
        _processData = new InventoryProcessData();
        _statsSubject = new Subject<InventoryStatistics?>();
        
        _inventoryService = new Mock<IInventoryService>();
        _inventoryService.SetupGet(x => x.InventoryProcessData).Returns(_processData);
        
        _statsService = new Mock<IInventoryStatisticsService>();
        _statsService.SetupGet(x => x.Statistics).Returns(_statsSubject.AsObservable());
        
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
        _logger = new Mock<ILogger<InventoryMainStatusViewModel>>();
    }
    
    private InventoryMainStatusViewModel CreateVm()
    {
        var vm = new InventoryMainStatusViewModel(
            _inventoryService.Object,
            _sessionService.Object,
            _timeTrackingCache.Object,
            _dialogService.Object,
            _logger.Object,
            _statsService.Object);
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
        
        _statsSubject.OnNext(new InventoryStatistics { Errors = 3, Success = 0, TotalAnalyzed = 0 });
        
        vm.GlobalMainIcon.Should().Be("RegularError");
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void SuccessThenStatsWithoutErrors_ShowsCheck_AndStopsSpinner()
    {
        var vm = CreateVm();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        vm.IsInventoryInProgress.Should().BeTrue();
        
        _statsSubject.OnNext(new InventoryStatistics { Errors = 0, Success = 10, TotalAnalyzed = 10 });
        
        vm.GlobalMainIcon.Should().Be("SolidCheckCircle");
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void StatsBeforeSuccessWithErrors_RendersFinalImmediatelyOnSuccess()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(new InventoryStatistics { Errors = 2, Success = 0, TotalAnalyzed = 2 });
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.GlobalMainIcon.Should().Be("RegularError");
        vm.IsInventoryInProgress.Should().BeFalse();
        vm.GlobalMainStatusText.Should().NotBeNull();
    }
    
    [Test]
    public void StatsBeforeSuccessWithoutErrors_RendersCheckImmediatelyOnSuccess()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(new InventoryStatistics { Errors = 0, Success = 5, TotalAnalyzed = 5 });
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
        
        _statsSubject.OnNext(new InventoryStatistics { Errors = 0, Success = 3, TotalAnalyzed = 3 });
        
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
        
        _statsSubject.OnNext(new InventoryStatistics { Errors = 4, Success = 0, TotalAnalyzed = 4 });
        vm.HasErrors.Should().BeTrue();
        
        _statsSubject.OnNext(new InventoryStatistics { Errors = 0, Success = 10, TotalAnalyzed = 10 });
        vm.HasErrors.Should().BeFalse();
    }
    
    [Test]
    public void SessionPreparation_ResetsStatisticsAndVisuals()
    {
        var vm = CreateVm();
        
        _statsSubject.OnNext(new InventoryStatistics { Errors = 1, Success = 2, TotalAnalyzed = 3 });
        vm.GlobalAnalyzeErrors.Should().Be(1);
        
        vm.GlobalMainIcon = "SolidCheckCircle";
        vm.GlobalMainStatusText = "dummy";
        vm.GlobalMainIconBrush = null; // not asserting specific brush here
        
        _sessionStatusSubject.OnNext(SessionStatus.Preparation);
        
        vm.GlobalTotalAnalyzed.Should().Be(0);
        vm.GlobalAnalyzeSuccess.Should().Be(0);
        vm.GlobalAnalyzeErrors.Should().Be(0);
        vm.GlobalMainIcon.Should().Be("None");
        vm.GlobalMainStatusText.Should().BeEmpty();
    }
}