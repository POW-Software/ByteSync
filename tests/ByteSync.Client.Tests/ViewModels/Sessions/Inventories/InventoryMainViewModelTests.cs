using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.ViewModels.Sessions.Inventories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Sessions.Inventories;

[TestFixture]
public class InventoryMainViewModelTests
{
    private InventoryProcessData _processData = null!;
    private Mock<IInventoryService> _inventoryService = null!;
    
    [SetUp]
    public void Setup()
    {
        _processData = new InventoryProcessData();
        _inventoryService = new Mock<IInventoryService>();
        _inventoryService.SetupGet(x => x.InventoryProcessData).Returns(_processData);
    }
    
    private InventoryMainViewModel CreateVm(
        InventoryMainStatusViewModel? statusVm = null,
        InventoryIdentificationViewModel? idVm = null,
        InventoryDeltaGenerationViewModel? deltaVm = null,
        InventoryBeforeStartViewModel? beforeVm = null)
    {
        statusVm ??= new InventoryMainStatusViewModel();
        idVm ??= new InventoryIdentificationViewModel();
        deltaVm ??= new InventoryDeltaGenerationViewModel();
        beforeVm ??= new InventoryBeforeStartViewModel();
        
        var vm = new InventoryMainViewModel(
            statusVm,
            idVm,
            deltaVm,
            beforeVm,
            _inventoryService.Object);
        vm.Activator.Activate();
        
        return vm;
    }
    
    [Test]
    public void HasLocalInventoryStarted_IsFalse_WhenPending_True_WhenRunning()
    {
        var vm = CreateVm();
        
        _processData.MainStatus.OnNext(InventoryTaskStatus.Pending);
        vm.HasLocalInventoryStarted.Should().BeFalse();
        
        _processData.MainStatus.OnNext(InventoryTaskStatus.Running);
        vm.HasLocalInventoryStarted.Should().BeTrue();
    }
    
    [TestCase(InventoryTaskStatus.Success)]
    [TestCase(InventoryTaskStatus.Cancelled)]
    [TestCase(InventoryTaskStatus.Error)]
    [TestCase(InventoryTaskStatus.NotLaunched)]
    public void HasLocalInventoryStarted_IsTrue_OnAnyNonPending(InventoryTaskStatus status)
    {
        var vm = CreateVm();
        
        _processData.MainStatus.OnNext(status);
        vm.HasLocalInventoryStarted.Should().BeTrue();
    }
    
    [Test]
    public void HasLocalInventoryStarted_TogglesBackToFalse_WhenReturningToPending()
    {
        var vm = CreateVm();
        
        _processData.MainStatus.OnNext(InventoryTaskStatus.Running);
        vm.HasLocalInventoryStarted.Should().BeTrue();
        
        _processData.MainStatus.OnNext(InventoryTaskStatus.Pending);
        vm.HasLocalInventoryStarted.Should().BeFalse();
    }
    
    [Test]
    public void Constructor_AssignsDependencies_AndProcessData()
    {
        var statusVm = new InventoryMainStatusViewModel();
        var idVm = new InventoryIdentificationViewModel();
        var deltaVm = new InventoryDeltaGenerationViewModel();
        var beforeVm = new InventoryBeforeStartViewModel();
        
        var vm = CreateVm(statusVm, idVm, deltaVm, beforeVm);
        
        vm.InventoryMainStatusViewModel.Should().BeSameAs(statusVm);
        vm.InventoryIdentificationViewModel.Should().BeSameAs(idVm);
        vm.InventoryDeltaGenerationViewModel.Should().BeSameAs(deltaVm);
        vm.InventoryBeforeStartViewModel.Should().BeSameAs(beforeVm);
        vm.InventoryProcessData.Should().BeSameAs(_processData);
    }
}