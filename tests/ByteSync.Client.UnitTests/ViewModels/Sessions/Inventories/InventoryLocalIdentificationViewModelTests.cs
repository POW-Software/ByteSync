using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.Inventories;
using ByteSync.Business.Themes;
using ByteSync.Client.UnitTests.Helpers;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.ViewModels.Sessions.Inventories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Inventories;

[TestFixture]
public class InventoryLocalIdentificationViewModelTests
{
    private InventoryProcessData _processData = null!;
    private Mock<IInventoryService> _inventoryService = null!;
    private Mock<IThemeService> _themeService = null!;
    private IBrush _backgroundBrush = null!;
    private IBrush _secondaryBrush = null!;
    
    [SetUp]
    public void Setup()
    {
        _processData = new InventoryProcessData();
        
        _inventoryService = new Mock<IInventoryService>();
        _inventoryService.SetupGet(x => x.InventoryProcessData).Returns(_processData);
        
        _backgroundBrush = Mock.Of<IBrush>();
        _secondaryBrush = Mock.Of<IBrush>();
        
        _themeService = new Mock<IThemeService>(MockBehavior.Strict);
        _themeService.Setup(x => x.GetBrush("HomeCloudSynchronizationBackGround")).Returns(_backgroundBrush);
        _themeService.Setup(x => x.GetBrush("MainSecondaryColor")).Returns(_secondaryBrush);
        _themeService.SetupGet(x => x.SelectedTheme)
            .Returns(Observable.Never<Theme>());
    }
    
    [Test]
    public void StatusTransitions_UpdateIconsBrushesAndText()
    {
        var vm = CreateVm();
        
        vm.IdentificationIcon.Should().Be("RegularPauseCircle");
        vm.IdentificationIconBrush.Should().BeSameAs(_backgroundBrush);
        _processData.IdentificationStatus.OnNext(InventoryTaskStatus.Pending);
        
        _processData.IdentificationStatus.OnNext(InventoryTaskStatus.Running);
        
        vm.IdentificationStatus.Should().Be(InventoryTaskStatus.Running);
        vm.IsIdentificationRunning.Should().BeTrue();
        vm.IdentificationIcon.Should().Be("None");
        vm.IdentificationIconBrush.Should().BeSameAs(_backgroundBrush);
        vm.IdentificationStatusText.Should().NotBeNullOrWhiteSpace();
        
        _processData.IdentificationStatus.OnNext(InventoryTaskStatus.Cancelled);
        
        vm.IdentificationStatus.Should().Be(InventoryTaskStatus.Cancelled);
        vm.IsIdentificationRunning.Should().BeFalse();
        vm.IdentificationIcon.Should().Be("SolidXCircle");
        vm.IdentificationIconBrush.Should().BeSameAs(_secondaryBrush);
        var cancelledText = vm.IdentificationStatusText;
        cancelledText.Should().NotBeNullOrWhiteSpace();
        
        _processData.IdentificationStatus.OnNext(InventoryTaskStatus.Success);
        
        vm.IdentificationStatus.Should().Be(InventoryTaskStatus.Success);
        vm.IdentificationIcon.Should().Be("SolidCheckCircle");
        vm.IdentificationIconBrush.Should().BeSameAs(_backgroundBrush);
        var successText = vm.IdentificationStatusText;
        successText.Should().NotBeNullOrWhiteSpace();
        successText.Should().NotBe(cancelledText);
        
        // Simulate a running identification then a main error
        _processData.IdentificationStatus.OnNext(InventoryTaskStatus.Running);
        _processData.MainStatus.OnNext(InventoryTaskStatus.Error);
        
        vm.ShouldEventuallyBe(x => x.IdentificationStatus, InventoryTaskStatus.Error);
        vm.IdentificationIcon.Should().Be("SolidXCircle");
        vm.IdentificationIconBrush.Should().BeSameAs(_secondaryBrush);
        vm.IdentificationStatusText.Should().NotBeNullOrWhiteSpace();
        vm.IdentificationStatusText.Should().NotBe(successText);
    }

    [Test]
    public void MonitorUpdates_ShouldUpdateSkippedEntriesAndVisibility()
    {
        var vm = CreateVm();

        vm.SkippedEntriesCount.Should().Be(0);
        vm.ShowSkippedEntriesCount.Should().BeFalse();

        _processData.UpdateMonitorData(m => { m.SkippedEntriesCount = 3; });

        vm.ShouldEventuallyBe(x => x.SkippedEntriesCount, 3);
        vm.ShouldEventuallyBe(x => x.ShowSkippedEntriesCount, true);

        _processData.UpdateMonitorData(m => { m.SkippedEntriesCount = 0; });

        vm.ShouldEventuallyBe(x => x.ShowSkippedEntriesCount, false);
    }
    
    private InventoryLocalIdentificationViewModel CreateVm()
    {
        var vm = new InventoryLocalIdentificationViewModel(_inventoryService.Object, _themeService.Object);
        vm.Activator.Activate();
        
        return vm;
    }
}
