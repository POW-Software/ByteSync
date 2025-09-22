using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.TestsCommon;
using ByteSync.ViewModels.Sessions.Inventories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Sessions.Inventories;

[TestFixture]
public class TestInventoryProcessViewModel : AbstractTester
{
    private Mock<InventoryMainStatusViewModel> _mockInventoryMainStatusViewModel;
    private Mock<InventoryIdentificationViewModel> _mockInventoryIdentificationViewModel;
    private Mock<InventoryAnalysisViewModel> _mockInventoryAnalysisViewModel;
    private Mock<InventoryBeforeStartViewModel> _mockInventoryBeforeStartViewModel;
    private Mock<ISessionService> _mockSessionService;
    private Mock<IInventoryService> _mockInventoryService;
    private Mock<IDialogService> _mockDialogService;
    private Mock<ILogger<InventoryProcessViewModel>> _mockLogger;
    
    private InventoryProcessViewModel _inventoryProcessViewModel;
    
    
    [SetUp]
    public void SetUp()
    {
        _mockInventoryMainStatusViewModel = new Mock<InventoryMainStatusViewModel>();
        _mockInventoryIdentificationViewModel = new Mock<InventoryIdentificationViewModel>();
        _mockInventoryAnalysisViewModel = new Mock<InventoryAnalysisViewModel>();
        _mockInventoryBeforeStartViewModel = new Mock<InventoryBeforeStartViewModel>();
        _mockSessionService = new Mock<ISessionService>();
        _mockInventoryService = new Mock<IInventoryService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockLogger = new Mock<ILogger<InventoryProcessViewModel>>();
        
        _inventoryProcessViewModel = new InventoryProcessViewModel(
            _mockInventoryMainStatusViewModel.Object,
            _mockInventoryIdentificationViewModel.Object,
            _mockInventoryAnalysisViewModel.Object,
            _mockInventoryBeforeStartViewModel.Object,
            _mockInventoryService.Object,
            _mockDialogService.Object,
            _mockLogger.Object
        );
    }
    
    // [Test]
    // public void Test_1()
    // {
    //     new TestScheduler().With(sheduler => 
    //     {
    //         // Code run in this block will have both RxApp.MainThreadScheduler
    //         // and RxApp.TaskpoolScheduler assigned to the new TestScheduler.
    //         
    //         // http://introtorx.com/Content/v1.0.10621.0/16_TestingRx.html
    //         // https://www.reactiveui.net/docs/handbook/scheduling/
    //         // https://www.reactiveui.net/docs/handbook/testing/
    //         
    //         InventoryProcessViewModel inventoryProcessViewModel = new InventoryProcessViewModel(Mock.Of<ISessionService?>(),
    //             Mock.Of<IInventoryService>(), 
    //             Mock.Of<ICloudSessionEventsHub>(), Mock.Of<IDialogService>());
    //     });
    // }
    
    // [Test]
    // public void Test_IsRunning()
    // {
    //     new TestScheduler().With(sheduler =>
    //     {
    //         // As it's hard to pass lambda as tests cases, we use a loop
    //         ISubject<LocalInventoryPartStatus> subject;
    //         Func<InventoryProcessViewModel, bool> property;
    //
    //         InventoryProcessData inventoryProcessData = new InventoryProcessData();
    //         
    //         var sessionDataHolder = new Mock<ISessionService>();
    //         var inventoriesService = new Mock<IInventoryService>();
    //         inventoriesService.SetupGet(o => o.InventoryProcessData)
    //             .Returns(inventoryProcessData);
    //         
    //         
    //         
    //         // todo 050523
    //         // MakeAsserts(inventoryProcessData.MainStatus, model => model.IsInventoryRunning, sessionDataHolder, inventoriesService);
    //         // MakeAsserts(inventoryProcessData.AnalysisStatus, model => model.IsAnalysisRunning, sessionDataHolder, inventoriesService);
    //         // MakeAsserts(inventoryProcessData.IdentificationStatus, model => model.IsIdentificationRunning, sessionDataHolder, inventoriesService);
    //     });
    //
    //     void MakeAsserts(ISubject<LocalInventoryPartStatus> subject, Func<InventoryProcessViewModel, bool> property,
    //         Mock<ISessionService> sessionDataHolder, Mock<IInventoryService> inventoriesService)
    //     {
    //         InventoryProcessViewModel inventoryProcessViewModel = new InventoryProcessViewModel(sessionDataHolder.Object, inventoriesService.Object, 
    //             Mock.Of<ICloudSessionEventsHub>(), Mock.Of<IDialogService>());
    //
    //         inventoryProcessViewModel.Activator.Activate();
    //
    //         // subject = inventoryProcessData.MainStatus;
    //         // property = model => model.IsInventoryRunning;
    //         
    //         // La base
    //         ClassicAssert.IsFalse(property.Invoke(inventoryProcessViewModel));
    //
    //         subject.OnNext(LocalInventoryPartStatus.Running);
    //         ClassicAssert.IsTrue(property.Invoke(inventoryProcessViewModel));
    //
    //         subject.OnNext(LocalInventoryPartStatus.Success);
    //         ClassicAssert.IsFalse(property.Invoke(inventoryProcessViewModel));
    //
    //         // Puis on passe toute l'enum
    //         foreach (var enumMember in Enum.GetValues<LocalInventoryPartStatus>())
    //         {
    //             subject.OnNext(enumMember);
    //
    //             if (enumMember == LocalInventoryPartStatus.Running)
    //             {
    //                 ClassicAssert.IsTrue(property.Invoke(inventoryProcessViewModel));
    //             }
    //             else
    //             {
    //                 ClassicAssert.IsFalse(property.Invoke(inventoryProcessViewModel));
    //             }
    //         }
    //     }
    // }
}