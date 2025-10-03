using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.ViewModels.Sessions.Inventories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Inventories;

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
        InventoryGlobalStatusViewModel? globalVm = null,
        InventoryLocalStatusViewModel? localVm = null,
        InventoryLocalIdentificationViewModel? idVm = null,
        InventoryDeltaGenerationViewModel? deltaVm = null,
        InventoryBeforeStartViewModel? beforeVm = null)
    {
        globalVm ??= new InventoryGlobalStatusViewModel();
        localVm ??= new InventoryLocalStatusViewModel();
        idVm ??= new InventoryLocalIdentificationViewModel();
        deltaVm ??= new InventoryDeltaGenerationViewModel();
        beforeVm ??= new InventoryBeforeStartViewModel();
        
        var vm = new InventoryMainViewModel(
            globalVm,
            localVm,
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
        var globalVm = new InventoryGlobalStatusViewModel();
        var localVm = new InventoryLocalStatusViewModel();
        var idVm = new InventoryLocalIdentificationViewModel();
        var deltaVm = new InventoryDeltaGenerationViewModel();
        var beforeVm = new InventoryBeforeStartViewModel();
        
        var vm = CreateVm(globalVm, localVm, idVm, deltaVm, beforeVm);
        
        vm.InventoryGlobalStatusViewModel.Should().BeSameAs(globalVm);
        vm.InventoryLocalStatusViewModel.Should().BeSameAs(localVm);
        vm.InventoryLocalIdentificationViewModel.Should().BeSameAs(idVm);
        vm.InventoryDeltaGenerationViewModel.Should().BeSameAs(deltaVm);
        vm.InventoryBeforeStartViewModel.Should().BeSameAs(beforeVm);
        vm.InventoryProcessData.Should().BeSameAs(_processData);
    }
}