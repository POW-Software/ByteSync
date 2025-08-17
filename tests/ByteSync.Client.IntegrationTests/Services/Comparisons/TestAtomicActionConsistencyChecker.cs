using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
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
using ByteSync.Services.Comparisons;
using ByteSync.Services.Sessions;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;
using NUnit.Framework.Legacy;

namespace ByteSync.Client.IntegrationTests.Services.Comparisons;

public class TestAtomicActionConsistencyChecker : IntegrationTest
{
    private AtomicActionConsistencyChecker _atomicActionConsistencyChecker;
    private ComparisonResultPreparer _comparisonResultPreparer;

    [SetUp]
    public void Setup()
    {
        RegisterType<CloudSessionLocalDataManager, ICloudSessionLocalDataManager>();
        RegisterType<ComparisonResultPreparer>();
        RegisterType<AtomicActionConsistencyChecker>();
        RegisterType<ComparisonItemViewModelFactory>();
        BuildMoqContainer();

        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        contextHelper.GenerateCurrentEndpoint();
        var testDirectory = _testDirectoryService.CreateTestDirectory();

        var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironmentService.Setup(m => m.AssemblyFullName)
            .Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));

        var mockLocalApplicationDataManager = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalApplicationDataManager.Setup(m => m.ApplicationDataPath).Returns(IOUtils.Combine(
            testDirectory.FullName,
            "ApplicationDataPath"));

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(It.IsAny<ComparisonItem>()))
            .Returns(new List<AtomicAction>());

        _testDirectoryService.CreateTestDirectory();
        _comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        _atomicActionConsistencyChecker = Container.Resolve<AtomicActionConsistencyChecker>();
    }

    [Test]
    public async Task Test_DirectoryOnlyOnA_1()
    {
        AtomicAction atomicAction;
        AtomicActionConsistencyCheckCanAddResult checkResult;
        ComparisonResult comparisonResult;

        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        dataA.CreateSubdirectory("Dir1");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();

        comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        atomicAction = new AtomicAction
        {
            Operator = ActionOperatorTypes.Create,
            Destination = inventoryDataB.GetSingleDataPart()
        };

        checkResult = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonResult.ComparisonItems);
        ClassicAssert.IsTrue(checkResult.IsOK);
    }


    [Test]
    public async Task Test_DirectoryOnlyOnA_2()
    {
        AtomicAction atomicAction;
        AtomicActionConsistencyCheckCanAddResult checkResult;

        ComparisonResult comparisonResult;

        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        dataA.CreateSubdirectory("Dir1");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();

        comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        atomicAction = new AtomicAction
        {
            Operator = ActionOperatorTypes.Create,
            Destination = inventoryDataA.GetSingleDataPart() // On demande la création sur A, ça ne peut passer
        };

        checkResult = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonResult.ComparisonItems);
        ClassicAssert.IsFalse(checkResult.IsOK);
    }


    [Test]
    [TestCase(DataTypes.Directories, LinkingKeys.RelativePath, 1)]
    [TestCase(DataTypes.Files, LinkingKeys.RelativePath, 1)]
    [TestCase(DataTypes.FilesDirectories, LinkingKeys.RelativePath, 2)]
    [TestCase(DataTypes.Directories, LinkingKeys.Name, 1)]
    [TestCase(DataTypes.Files, LinkingKeys.Name, 1)]
    [TestCase(DataTypes.FilesDirectories, LinkingKeys.Name, 2)]
    public async Task Test_DirectoryOnlyOnA_3(DataTypes dataType, LinkingKeys linkingKey,
        int comparisonItemViewModelsCount)
    {
        // Dans ce test, inventoryDataB est de type File
        // On ne peut donc pas copier Dir1 vers B

        AtomicAction atomicAction;

        ComparisonResult comparisonResult;

        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        dataA.CreateSubdirectory("Dir1");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var fileB = _testDirectoryService.CreateFileInDirectory(dataB, "fileB.txt", "fileBContent___");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(fileB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings(dataType, linkingKey);

        comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        // List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
        // ClassicAssert.AreEqual(comparisonItemViewModelsCount, comparisonItemViewModels.Count);

        atomicAction = new AtomicAction
        {
            Operator = ActionOperatorTypes.Create,
            Destination = inventoryDataB.GetSingleDataPart()
        };


        var checkResult = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonResult.ComparisonItems);

        checkResult.IsOK.Should().BeFalse();
        checkResult.ValidValidations.Count.Should().Be(0);
        checkResult.FailedValidations.Count.Should().Be(comparisonItemViewModelsCount);

        // Vérification que chaque échec a une raison spécifique
        checkResult.FailedValidations.Should().OnlyContain(f => f.FailureReason != null);
        // ClassicAssert.IsFalse(checkResult.IsOK);
        // ClassicAssert.AreEqual(0, checkResult.ValidComparisons.Count);
        // ClassicAssert.AreEqual(comparisonItemViewModelsCount, checkResult.NonValidComparisons.Count);
    }


    [Test]
    public async Task Test_FileOnlyOnA_1()
    {
        // Dans ce test, inventoryDataB est de type File
        // On ne peut donc pas copier fileA.txt vers B car il n'y a pas de répertoire défini pour recevoir le fichier fileA.txt

        AtomicAction atomicAction;

        ComparisonResult comparisonResult;

        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var fileA = _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "fileAContent");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        var fileB = _testDirectoryService.CreateFileInDirectory(dataB, "fileB.txt", "fileBContent___");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(fileB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();

        comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        atomicAction = new AtomicAction
        {
            Source = inventoryDataA.GetSingleDataPart(),
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Destination = inventoryDataB.GetSingleDataPart() // On demande la création sur B, ça ne peut passer
        };

        var checkResult = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonResult.ComparisonItems);

        checkResult.IsOK.Should().BeFalse();
        checkResult.ValidValidations.Count.Should().Be(0);
        checkResult.FailedValidations.Count.Should().Be(2);
        
        checkResult.FailedValidations.Should().OnlyContain(f => f.FailureReason != null);
    }

    [Test]
    [TestCase(ActionOperatorTypes.SynchronizeContentAndDate, 0)]
    [TestCase(ActionOperatorTypes.SynchronizeContentOnly, 0)]
    [TestCase(ActionOperatorTypes.SynchronizeDate, 0)]
    public async Task Test_FileOnAAndB_SameContentAndDate(ActionOperatorTypes actionOperator, int expectedValidItems)
    {
        AtomicAction atomicAction;
        AtomicActionConsistencyChecker atomicActionConsistencyChecker;

        ComparisonResult comparisonResult;

        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        var fileA = _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "fileAContent");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        fileA.CopyTo(dataB.Combine(fileA.Name));

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();

        comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        atomicAction = new AtomicAction
        {
            Source = inventoryDataA.GetSingleDataPart(),
            Operator = actionOperator,
            Destination = inventoryDataB.GetSingleDataPart()
        };

        var checkResult = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonResult.ComparisonItems);
        if (expectedValidItems == 0)
            checkResult.IsOK.Should().BeFalse();
        else
            checkResult.IsOK.Should().BeTrue();

        checkResult.ValidValidations.Count.Should().Be(expectedValidItems);
        checkResult.FailedValidations.Count.Should().Be(1 - expectedValidItems);
    }
}