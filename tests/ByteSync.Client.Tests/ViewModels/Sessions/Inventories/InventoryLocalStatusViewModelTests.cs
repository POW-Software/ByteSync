using System.Reactive.Subjects;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Sessions.Inventories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Sessions.Inventories;

[TestFixture]
public class InventoryLocalStatusViewModelTests
{
    private InventoryProcessData _processData = null!;
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ITimeTrackingCache> _timeTrackingCache = null!;
    private Mock<IInventoryService> _inventoryService = null!;
    private Subject<TimeTrack> _timeTrackSubject = null!;
    
    [SetUp]
    public void Setup()
    {
        _processData = new InventoryProcessData();
        
        _sessionService = new Mock<ISessionService>();
        _sessionService.SetupGet(x => x.SessionId).Returns("test-session");
        
        _inventoryService = new Mock<IInventoryService>();
        _inventoryService.SetupGet(x => x.InventoryProcessData).Returns(_processData);
        
        _timeTrackSubject = new Subject<TimeTrack>();
        
        var mockComputer = new Mock<ITimeTrackingComputer>();
        mockComputer.SetupGet(x => x.RemainingTime).Returns(_timeTrackSubject);
        
        _timeTrackingCache = new Mock<ITimeTrackingCache>();
        _timeTrackingCache
            .Setup(x => x.GetTimeTrackingComputer(It.IsAny<string>(), It.IsAny<TimeTrackingComputerType>()))
            .ReturnsAsync(mockComputer.Object);
    }
    
    private InventoryLocalStatusViewModel CreateVm()
    {
        var vm = new InventoryLocalStatusViewModel(
            _sessionService.Object,
            _timeTrackingCache.Object,
            _inventoryService.Object);
        vm.Activator.Activate();
        
        return vm;
    }
    
    [Test]
    public void EstimatedProcessEndName_Toggles_To_End_On_Terminal_Status()
    {
        var vm = CreateVm();
        
        vm.EstimatedProcessEndName.Should().NotBeNullOrEmpty();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Running);
        vm.EstimatedProcessEndName.Should().NotBeNullOrEmpty();
        
        _processData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        vm.EstimatedProcessEndName.Should().NotBeNullOrEmpty();
        vm.EstimatedProcessEndName.Should().NotBeEmpty();
    }
    
    [Test]
    public void TimeTracking_Flows_From_Computed_RemainingTime()
    {
        var vm = CreateVm();
        
        var now = DateTime.Now;
        var tt = new TimeTrack
        {
            StartDateTime = now.AddMinutes(-5),
            EstimatedEndDateTime = now.AddMinutes(10),
            RemainingTime = TimeSpan.FromMinutes(10)
        };
        
        _timeTrackSubject.OnNext(tt);
        
        vm.StartDateTime.Should().BeCloseTo(tt.StartDateTime!.Value, TimeSpan.FromSeconds(1));
        vm.EstimatedEndDateTime.Should().BeCloseTo(tt.EstimatedEndDateTime!.Value, TimeSpan.FromSeconds(1));
        vm.RemainingTime.Should().Be(tt.RemainingTime);
        vm.ElapsedTime.Should().BeGreaterThan(TimeSpan.Zero);
    }
}