using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Helpers;
using ByteSync.Factories.ViewModels;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Repositories;
using ByteSync.Services.Actions;
using ByteSync.Services.Sessions;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;
using NUnit.Framework.Legacy;

namespace ByteSync.Client.IntegrationTests.Services.Actions;


[TestFixture]
public class TestSharedAtomicActionComputer : IntegrationTest
{
    private ComparisonResultPreparer _comparisonResultPreparer;
    private SharedAtomicActionComputer _sharedAtomicActionComputer;

    [SetUp]
    public void Setup()
    {
        RegisterType<CloudSessionLocalDataManager, ICloudSessionLocalDataManager>();
        RegisterType<ComparisonResultPreparer>();
        RegisterType<SharedAtomicActionComputer>();
        RegisterType<AtomicActionRepository, IAtomicActionRepository>();
        RegisterType<SharedAtomicActionRepository, ISharedAtomicActionRepository>();
        RegisterType<ComparisonItemViewModelFactory>();
        BuildMoqContainer();
        
        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        contextHelper.GenerateCurrentEndpoint();
        var testDirectory = _testDirectoryService.CreateTestDirectory();

        var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironmentService.Setup(m => m.AssemblyFullName).Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));

        var mockLocalApplicationDataManager = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalApplicationDataManager.Setup(m => m.ApplicationDataPath).Returns(IOUtils.Combine(testDirectory.FullName, 
            "ApplicationDataPath"));
        
        // var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        // atomicActionRepository.Setup(m => m.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());

        _testDirectoryService.CreateTestDirectory();
        _comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        _sharedAtomicActionComputer = Container.Resolve<SharedAtomicActionComputer>();
    }
    
    [Test]
    public async Task Test_Empty()
    {
        var sharedAtomicActions = await _sharedAtomicActionComputer.ComputeSharedAtomicActions();
        sharedAtomicActions.Count.Should().Be(0);
    }
    
    [Test]
    public async Task Test_OneComparisonItem()
    {
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "file1.txt", "contentA");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        _testDirectoryService.CreateFileInDirectory(dataB, "file1.txt", "contentB_");

        InventoryData inventoryDataA = new InventoryData(dataA);
        InventoryData inventoryDataB = new InventoryData(dataB);

        SessionSettings sessionSettings = SessionSettingsGenerator.GenerateSessionSettings(analysisMode: AnalysisModes.Smart);
        
        ComparisonResult comparisonResult = await _comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);

        // List<ComparisonItemViewModel> comparisonItemViewModels = new List<ComparisonItemViewModel>();
        // foreach (var comparisonItem in comparisonResult.ComparisonItems)
        // {
        //     ComparisonItemViewModel comparisonItemViewModel = new ComparisonItemViewModel(comparisonItem,
        //         new List<Inventory> { inventoryDataA.Inventory, inventoryDataB.Inventory }, _mockObjectsGenerator.SessionDataHolder.Object);
        //
        //     comparisonItemViewModels.Add(comparisonItemViewModel);
        // }

        var atomicActions = new List<AtomicAction>()
        {
            new()
            {
                AtomicActionId = $"AAID_{Guid.NewGuid()}",
                Operator = ActionOperatorTypes.SynchronizeContentAndDate,
                Source = inventoryDataA.GetSingleDataPart(),
                Destination = inventoryDataB.GetSingleDataPart(),
                ComparisonItem = comparisonResult.ComparisonItems.First()
            }
        };

        var atomicActionRepository = Container.Resolve<IAtomicActionRepository>();
        atomicActionRepository.AddOrUpdate(atomicActions);
        // atomicActionRepository.Setup(m => m.Elements).Returns(atomicActions);
        
        
        // var synchronizationActionViewModel = BuildSynchronizationAction(ActionOperatorTypes.SynchronizeContentAndDate, inventoryDataA, inventoryDataB);
        // comparisonItemViewModels.First().TD_SynchronizationActions.Add(synchronizationActionViewModel);

        // var synchronizationActionConverter = BuildSharedAtomicActionComputer(comparisonItemViewModels);
        var sharedAtomicActions = await _sharedAtomicActionComputer.ComputeSharedAtomicActions();

        ClassicAssert.AreEqual(1, sharedAtomicActions.Count);
        var sharedAtomicAction = sharedAtomicActions.Single();

        ClassicAssert.IsNotEmpty(inventoryDataA.AllFileDescriptions[0].SignatureGuid);
        ClassicAssert.AreEqual(inventoryDataA.AllFileDescriptions[0].SignatureGuid, sharedAtomicAction.Source!.SignatureGuid);

        ClassicAssert.IsNotEmpty(inventoryDataB.AllFileDescriptions[0].SignatureGuid);
        ClassicAssert.AreEqual(inventoryDataB.AllFileDescriptions[0].SignatureGuid, sharedAtomicAction.Target!.SignatureGuid);
    }

    /*
    [Test]
    public async Task Test_OneComparisonItem_DoNothing()
    {
        CreateTestDirectory();

        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        CreateFileInDirectory(dataA, "file1.txt", "contentA");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        CreateFileInDirectory(dataB, "file1.txt", "contentB_");

        InventoryData inventoryDataA = new InventoryData(dataA);
        InventoryData inventoryDataB = new InventoryData(dataB);

        SessionSettings sessionSettings = new SessionSettings();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;

        ComparisonResultPreparer comparisonResultPreparer = new ComparisonResultPreparer(TestDirectory);
        ComparisonResult comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);

        List<ComparisonItemViewModel> comparisonItemViewModels = new List<ComparisonItemViewModel>();
        foreach (var comparisonItem in comparisonResult.ComparisonItems)
        {
            ComparisonItemViewModel comparisonItemViewModel = new ComparisonItemViewModel(comparisonItem,
                new List<Inventory> { inventoryDataA.Inventory, inventoryDataB.Inventory }, _mockObjectsGenerator.SessionDataHolder.Object);

            comparisonItemViewModels.Add(comparisonItemViewModel);
        }

        var synchronizationActionViewModel = BuildSynchronizationAction(ActionOperatorTypes.SynchronizeContentAndDate, inventoryDataA, inventoryDataB);
        comparisonItemViewModels.First().TD_SynchronizationActions.Add(synchronizationActionViewModel);
        synchronizationActionViewModel = BuildSynchronizationAction(ActionOperatorTypes.DoNothing);
        comparisonItemViewModels.First().TD_SynchronizationActions.Add(synchronizationActionViewModel);

        var synchronizationActionConverter = BuildSharedAtomicActionComputer(comparisonItemViewModels);
        var sharedAtomicActions = synchronizationActionConverter.ComputeSharedAtomicActions();

        ClassicAssert.AreEqual(0, sharedAtomicActions.Count);
    }

    // private SynchronizationActionViewModel BuildSynchronizationAction(ActionOperatorTypes @operator)
    // {
    //
    //     throw new Exception("Review implementation!");
    //
    //     var atomicAction = new AtomicAction();
    //     atomicAction.AtomicActionId = $"AAID_{Guid.NewGuid()}";
    //
    //     atomicAction.Operator = @operator;
    //     atomicAction.Source = null;
    //     atomicAction.Destination = null;
    //
    //     SynchronizationActionViewModel synchronizationActionViewModel = new SynchronizationActionViewModel(atomicAction, _mockObjectsGenerator.CloudSessionEventsHub.Object, _mockObjectsGenerator.SessionDataHolder.Object);
    //
    //     return synchronizationActionViewModel;
    // }

    private SynchronizationActionViewModel BuildSynchronizationAction(ActionOperatorTypes @operator, InventoryData source, InventoryData target)
    {
        return BuildSynchronizationAction(@operator, source.InventoryParts.Single(), target.InventoryParts.Single());
    }

    // private SynchronizationActionViewModel BuildSynchronizationAction(ActionOperatorTypes @operator, InventoryPart source, InventoryPart target)
    // {
    //     // todo 050523
    //     throw new Exception("Review implementation!");
    //
    //     var atomicAction = new AtomicAction();
    //     atomicAction.AtomicActionId = $"AAID_{Guid.NewGuid()}";
    //
    //     atomicAction.Operator = @operator;
    //     atomicAction.Source = new DataPart(source.Code, source);
    //
    //     atomicAction.Destination = new DataPart(target.Code, target);
    //
    //     SynchronizationActionViewModel synchronizationActionViewModel = new SynchronizationActionViewModel(atomicAction, _mockObjectsGenerator.CloudSessionEventsHub.Object, _mockObjectsGenerator.SessionDataHolder.Object);
    //
    //     return synchronizationActionViewModel;
    // }

    [SetUp]
    public void Setup()
    {
        _mockObjectsGenerator = new MockObjectsGenerator(this);
        _mockObjectsGenerator.GenerateCloudSessionManager();
    }

    private SharedAtomicActionComputer BuildSharedAtomicActionComputer(
        params ComparisonItemViewModel[] comparisonItems)
    {
        List<ComparisonItemViewModel> comparisonItemsList = new List<ComparisonItemViewModel>(comparisonItems);

        return BuildSharedAtomicActionComputer(comparisonItemsList);
    }*/
    
    // private SharedAtomicActionComputer BuildSharedAtomicActionComputer(List<ComparisonItemViewModel> comparisonItems)
    // {
    //     // todo 050523
    //     throw new Exception("Review implementation!");
    //
    //     /*
    //     
    //     var finalComparisonItems = new ObservableCollectionExtended<ComparisonItemViewModel>(comparisonItems);
    //     
    //     _mockObjectsGenerator.ComparisonItemsService
    //         .Setup(d => d.ComparisonItems)
    //         .Returns(finalComparisonItems)
    //         .Verifiable();
    //         
    //         */
    //
    //
    //     /*
    //     _mockObjectsGenerator.SessionDataHolder
    //         .Setup(d => d.SessionMode)
    //         .Returns(SessionModes.Cloud)
    //         .Verifiable();
    //         */
    //
    //     /*
    //     SharedAtomicActionComputer atomicActionComputer
    //         = new SharedAtomicActionComputer(_mockObjectsGenerator.SessionDataHolder.Object);
    //     
    //     return atomicActionComputer;
    //     
    //     */
    // }
}