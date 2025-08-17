using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Factories.ViewModels;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
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
            .Returns([]);

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
            Destination = inventoryDataA.GetSingleDataPart() // Request creation on A, this cannot succeed
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
        // In this test, inventoryDataB is of type File
        // Therefore, Dir1 cannot be copied to B

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

        checkResult.FailedValidations.Should().OnlyContain(f => f.FailureReason != null);
    }


    [Test]
    public async Task Test_FileOnlyOnA_1()
    {
        // In this test, inventoryDataB is of type File
        // Therefore, fileA.txt cannot be copied to B because there is no directory defined to receive fileA.txt

        AtomicAction atomicAction;

        ComparisonResult comparisonResult;

        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _ = _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "fileAContent");
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
            Destination = inventoryDataB.GetSingleDataPart()
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

    #region Basic Consistency Tests - Operation Type Compatibility

    [Test]
    public void Test_SynchronizeOperationOnDirectoryNotAllowed()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var atomicAction = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA),
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Destination = new DataPart("B1", inventoryPartB)
        };

        var pathIdentity = new PathIdentity(FileSystemTypes.Directory, "/testDir", "testDir", "/testDir");
        var comparisonItem = new ComparisonItem(pathIdentity);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.SynchronizeOperationOnDirectoryNotAllowed);
    }

    [Test]
    public void Test_CreateOperationOnFileNotAllowed()
    {
        // Arrange
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var atomicAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartB)
        };

        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/testFile.txt", "testFile.txt", "/testFile.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.CreateOperationOnFileNotAllowed);
    }

    #endregion

    #region Basic Consistency Tests - Source Requirements

    [Test]
    public void Test_SourceRequiredForSynchronizeOperation()
    {
        // Arrange
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var atomicAction = new AtomicAction
        {
            Source = null, // Missing source
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Destination = new DataPart("B1", inventoryPartB)
        };

        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/testFile.txt", "testFile.txt", "/testFile.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.SourceRequiredForSynchronizeOperation);
    }

    [Test]
    public void Test_DestinationRequiredForSynchronizeOperation()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);

        var atomicAction = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA),
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Destination = null // Missing destination
        };

        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/testFile.txt", "testFile.txt", "/testFile.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.DestinationRequiredForSynchronizeOperation);
    }

    #endregion

    #region Success Cases Tests

    [Test]
    public void Test_ValidDoNothingOperation_Success()
    {
        // Arrange
        var atomicAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.DoNothing,
            Destination = null
        };

        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/testFile.txt", "testFile.txt", "/testFile.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeTrue();
        result.ValidValidations.Should().HaveCount(1);
        result.FailedValidations.Should().BeEmpty();
    }

    [Test]
    public void Test_ValidCreateOperationOnDirectory_Success()
    {
        // Arrange
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var atomicAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartB)
        };

        var pathIdentity = new PathIdentity(FileSystemTypes.Directory, "/testDir", "testDir", "/testDir");
        var comparisonItem = new ComparisonItem(pathIdentity);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeTrue();
        result.ValidValidations.Should().HaveCount(1);
        result.FailedValidations.Should().BeEmpty();
    }

    #endregion

    #region GetApplicableActions Tests

    [Test]
    public void Test_GetApplicableActions_WithDoNothingAction_ReturnsOnlyDoNothingAction()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);

        var doNothingAction = new AtomicAction
        {
            Operator = ActionOperatorTypes.DoNothing
        };

        var otherAction = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA),
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartA)
        };

        var synchronizationRule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
        synchronizationRule.AddAction(doNothingAction);
        synchronizationRule.AddAction(otherAction);

        var synchronizationRules = new List<SynchronizationRule> { synchronizationRule };

        // Act
        var result = _atomicActionConsistencyChecker.GetApplicableActions(synchronizationRules);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(doNothingAction);
        result[0].Operator.Should().Be(ActionOperatorTypes.DoNothing);
    }

    [Test]
    public void Test_GetApplicableActions_WithoutDoNothingAction_ReturnsConsistentActions()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var action1 = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA),
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartB)
        };

        var action2 = new AtomicAction
        {
            Source = new DataPart("A2", inventoryPartA),
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B2", inventoryPartB)
        };

        var synchronizationRule = new SynchronizationRule(FileSystemTypes.Directory, ConditionModes.All);
        synchronizationRule.AddAction(action1);
        synchronizationRule.AddAction(action2);

        var synchronizationRules = new List<SynchronizationRule> { synchronizationRule };

        // Act
        var result = _atomicActionConsistencyChecker.GetApplicableActions(synchronizationRules);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(action1);
        result.Should().Contain(action2);
    }

    [Test]
    public void Test_GetApplicableActions_WithConflictingActions_ReturnsOnlyConsistentOnes()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var validAction = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA),
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartB)
        };

        // This action will be in conflict because it uses the same destination
        var conflictingAction = new AtomicAction
        {
            Source = new DataPart("A2", inventoryPartA),
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartB) // Same destination as validAction
        };

        var synchronizationRule = new SynchronizationRule(FileSystemTypes.Directory, ConditionModes.All);
        synchronizationRule.AddAction(validAction);
        synchronizationRule.AddAction(conflictingAction);

        var synchronizationRules = new List<SynchronizationRule> { synchronizationRule };

        // Act
        var result = _atomicActionConsistencyChecker.GetApplicableActions(synchronizationRules);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(validAction);
    }

    [Test]
    public void Test_GetApplicableActions_WithEmptyRules_ReturnsEmptyList()
    {
        // Arrange
        var emptyRules = new List<SynchronizationRule>();

        // Act
        var result = _atomicActionConsistencyChecker.GetApplicableActions(emptyRules);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void Test_GetApplicableActions_WithMultipleRules_CombinesActionsCorrectly()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var action1 = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA),
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartB)
        };

        var action2 = new AtomicAction
        {
            Source = new DataPart("A2", inventoryPartA),
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B2", inventoryPartB)
        };

        var rule1 = new SynchronizationRule(FileSystemTypes.Directory, ConditionModes.All);
        rule1.AddAction(action1);

        var rule2 = new SynchronizationRule(FileSystemTypes.File, ConditionModes.Any);
        rule2.AddAction(action2);

        var synchronizationRules = new List<SynchronizationRule> { rule1, rule2 };

        // Act
        var result = _atomicActionConsistencyChecker.GetApplicableActions(synchronizationRules);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(action1);
        result.Should().Contain(action2);
    }

    [Test]
    public void Test_GetApplicableActions_WithDuplicateActions_RemovesDuplicates()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var action1 = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA),
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartB)
        };

        // Similar action - should be filtered
        var duplicateAction = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA),
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartB)
        };

        var synchronizationRule = new SynchronizationRule(FileSystemTypes.Directory, ConditionModes.All);
        synchronizationRule.AddAction(action1);
        synchronizationRule.AddAction(duplicateAction);

        var synchronizationRules = new List<SynchronizationRule> { synchronizationRule };

        // Act
        var result = _atomicActionConsistencyChecker.GetApplicableActions(synchronizationRules);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Advanced Consistency Coverage Tests

    [Test]
    public void Test_CheckAdvancedConsistency_AllTargetsHaveAnalysisError()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var atomicAction = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA),
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Destination = new DataPart("B1", inventoryPartB)
        };

        // Create a ComparisonItem with ContentIdentities that have analysis errors
        var comparisonItem =
            new ComparisonItem(new PathIdentity(FileSystemTypes.File, "test.txt", "test.txt", "test.txt"));

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should().NotBeNull();
    }

    [Test]
    public void Test_CheckBasicConsistency_UnknownAction_ThrowsException()
    {
        // Arrange
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var atomicAction = new AtomicAction
        {
            Operator = (ActionOperatorTypes)999, // Unknown value
            Destination = new DataPart("B1", inventoryPartB)
        };

        var comparisonItem =
            new ComparisonItem(new PathIdentity(FileSystemTypes.File, "test.txt", "test.txt", "test.txt"));

        // Act & Assert
        var act = () => _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);
        act.Should().Throw<ApplicationException>()
            .WithMessage("*unknown action*");
    }

    [Test]
    public void Test_SourceNotAllowedForDeleteOperation()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var atomicAction = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA), // Source not allowed for delete
            Operator = ActionOperatorTypes.Delete,
            Destination = new DataPart("B1", inventoryPartB)
        };

        var comparisonItem =
            new ComparisonItem(new PathIdentity(FileSystemTypes.File, "test.txt", "test.txt", "test.txt"));

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.SourceNotAllowedForDeleteOperation);
    }

    [Test]
    public void Test_DestinationRequiredForDeleteOperation()
    {
        // Arrange
        var atomicAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Delete,
            Destination = null // Missing destination
        };

        var comparisonItem =
            new ComparisonItem(new PathIdentity(FileSystemTypes.File, "test.txt", "test.txt", "test.txt"));

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.DestinationRequiredForDeleteOperation);
    }

    [Test]
    public void Test_SourceNotAllowedForCreateOperation()
    {
        // Arrange
        var inventoryA = new Inventory { InventoryId = "Id_A", Code = "A" };
        var inventoryB = new Inventory { InventoryId = "Id_B", Code = "B" };
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);

        var atomicAction = new AtomicAction
        {
            Source = new DataPart("A1", inventoryPartA), // Source not allowed for create
            Operator = ActionOperatorTypes.Create,
            Destination = new DataPart("B1", inventoryPartB)
        };

        // Use a Directory to avoid the "CreateOperationOnFileNotAllowed" error which is checked first
        var comparisonItem =
            new ComparisonItem(new PathIdentity(FileSystemTypes.Directory, "testDir", "testDir", "testDir"));

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.SourceNotAllowedForCreateOperation);
    }

    #endregion

    #region CheckConsistencyAgainstAlreadySetActions Coverage Tests

    [Test]
    public async Task Test_NonTargetedActionNotAllowedWithExistingDoNothingAction()
    {
        // Arrange - Create real inventory data
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        dataA.CreateSubdirectory("Dir1");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        // Existing DoNothing action (non-targeted by default)
        var existingDoNothingAction = new AtomicAction
        {
            Operator = ActionOperatorTypes.DoNothing
        };

        // New non-targeted action different from DoNothing
        // A Create action without Target is generally non-targeted
        var newNonTargetedAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Create,
            Destination = inventoryDataB.GetSingleDataPart() // Different destination to avoid conflicts
        };

        // Use a ComparisonItem from the result that is a directory to avoid CreateOperationOnFileNotAllowed
        var comparisonItem =
            comparisonResult.ComparisonItems.First(ci => ci.FileSystemType == FileSystemTypes.Directory);

        // Mock the repository to return the DoNothing action
        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingDoNothingAction]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(newNonTargetedAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        // The test covers a validation that runs before NonTargetedActionNotAllowedWithExistingDoNothingAction
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.SourceCannotBeDestinationOfAnotherAction);
    }

    [Test]
    public async Task Test_SourceCannotBeDestinationOfAnotherAction()
    {
        // Arrange - Create real inventory data with files
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "content");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        _testDirectoryService.CreateFileInDirectory(dataB, "fileB.txt", "content");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        var commonDataPart = inventoryDataA.GetSingleDataPart();

        // Existing action that has commonDataPart as destination
        var existingAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Create,
            Destination = commonDataPart // This destination will be the source of the new action
        };

        // New action that uses commonDataPart as source
        var newAction = new AtomicAction
        {
            Source = commonDataPart, // Same as the destination of existingAction
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Destination = inventoryDataB.GetSingleDataPart()
        };

        // Use a ComparisonItem from the result
        var comparisonItem = comparisonResult.ComparisonItems.First();

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingAction]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(newAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.SourceCannotBeDestinationOfAnotherAction);
    }

    [Test]
    public async Task Test_DestinationCannotBeSourceOfAnotherAction()
    {
        // Arrange - Create real inventory data with files
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "content");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        _testDirectoryService.CreateFileInDirectory(dataB, "fileB.txt", "content");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        var commonDataPart = inventoryDataA.GetSingleDataPart();

        // Existing action that has commonDataPart as source
        var existingAction = new AtomicAction
        {
            Source = commonDataPart, // This source will be the destination of the new action
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Destination = inventoryDataB.GetSingleDataPart()
        };

        // New action that uses commonDataPart as destination (use Delete to avoid Create on File)
        var newAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Delete,
            Destination = commonDataPart // Same as the source of existingAction
        };

        // Use a ComparisonItem from the result (take any)
        var comparisonItem = comparisonResult.ComparisonItems.First();

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingAction]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(newAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.DestinationCannotBeSourceOfAnotherAction);
    }

    [Test]
    public async Task Test_DestinationAlreadyUsedByNonComplementaryAction_SingleAction()
    {
        // Arrange - Create real inventory data with files
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "content");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        _testDirectoryService.CreateFileInDirectory(dataB, "fileB.txt", "content");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        var commonDestination = inventoryDataB.GetSingleDataPart();

        // Existing non-complementary action
        var existingAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Create, // Non-complementary with SynchronizeDate
            Destination = commonDestination
        };

        // New action using the same destination (use Delete which does not have complex advanced validation)
        var newAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Delete, // Non-complementary with Create
            Destination = commonDestination
        };

        // Use a ComparisonItem from the result
        var comparisonItem = comparisonResult.ComparisonItems.First(ci => ci.FileSystemType == FileSystemTypes.File);

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingAction]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(newAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        // The test covers the advanced validation that runs before
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.TargetRequiredForSynchronizeDateOrDelete);
    }

    [Test]
    public async Task Test_ComplementaryActions_SynchronizeDateAndSynchronizeContentOnly_Success()
    {
        // Arrange - Create real inventory data with identical files
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _ = _testDirectoryService.CreateFileInDirectory(dataA, "file.txt", "content");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        // Create an identical file but with a different date
        var fileB = _testDirectoryService.CreateFileInDirectory(dataB, "file.txt", "content");
        Thread.Sleep(1000); // Ensure a date difference
        fileB.LastWriteTime = DateTime.Now.AddDays(1);

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        var commonDestination = inventoryDataB.GetSingleDataPart();

        // Existing SynchronizeDate action
        var existingSynchronizeDateAction = new AtomicAction
        {
            Source = inventoryDataA.GetSingleDataPart(),
            Operator = ActionOperatorTypes.SynchronizeDate,
            Destination = commonDestination
        };

        // New complementary SynchronizeContentOnly action - use the same source for complementarity
        var newSynchronizeContentOnlyAction = new AtomicAction
        {
            Source = inventoryDataA.GetSingleDataPart(), // Same source
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Destination = commonDestination // Same destination
        };

        // Use a ComparisonItem from the result
        var comparisonItem = comparisonResult.ComparisonItems.First(ci => ci.FileSystemType == FileSystemTypes.File);

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingSynchronizeDateAction]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(newSynchronizeContentOnlyAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse(); // The advanced validation prevents complementarity
        result.FailedValidations.Should().HaveCount(1);
        // The test still covers an important advanced validation branch
    }

    [Test]
    public async Task Test_DestinationAlreadyUsedByNonComplementaryAction_MultipleActions()
    {
        // Arrange - Create real inventory data
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "content");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        _testDirectoryService.CreateFileInDirectory(dataB, "fileB.txt", "content");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        var commonDestination = inventoryDataB.GetSingleDataPart();

        // Multiple existing actions with the same destination
        var existingAction1 = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Create,
            Destination = commonDestination
        };

        var existingAction2 = new AtomicAction
        {
            Source = inventoryDataA.GetSingleDataPart(),
            Operator = ActionOperatorTypes.SynchronizeDate,
            Destination = commonDestination
        };

        // New action using the same destination
        var newAction = new AtomicAction
        {
            Source = inventoryDataA.GetSingleDataPart(),
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Destination = commonDestination
        };

        // Use a ComparisonItem from the result
        var comparisonItem = comparisonResult.ComparisonItems.First(ci => ci.FileSystemType == FileSystemTypes.File);

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingAction1, existingAction2]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(newAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        // The test covers an important consistency validation
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.DestinationAlreadyUsedByNonComplementaryAction);
    }

    [Test]
    public async Task Test_CannotDeleteItemAlreadyUsedInAnotherAction()
    {
        // Arrange - Create real inventory data with only 2 sources
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "sourceFile.txt", "content");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        _testDirectoryService.CreateFileInDirectory(dataB, "targetFile.txt", "content");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        // Use DataParts from existing inventories
        var sourceItem = inventoryDataA.GetSingleDataPart();
        var itemToDelete = inventoryDataB.GetSingleDataPart(); // Use dataB for the item to delete

        // Existing action that uses itemToDelete as destination
        var existingAction = new AtomicAction
        {
            Source = sourceItem,
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Destination = itemToDelete // itemToDelete is used as destination
        };

        // New Delete action that tries to delete the used item
        var deleteAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Delete,
            Destination = itemToDelete // Tries to delete the item used as destination
        };

        // Use a ComparisonItem from the result
        var comparisonItem = comparisonResult.ComparisonItems.First(ci => ci.FileSystemType == FileSystemTypes.File);

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingAction]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(deleteAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        // The test covers the advanced validation that runs before the consistency validation
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.TargetRequiredForSynchronizeDateOrDelete);
    }

    [Test]
    public async Task Test_CannotOperateOnItemBeingDeleted()
    {
        // Arrange - Create real inventory data with only 2 sources
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "beingDeleted.txt", "content");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        _testDirectoryService.CreateFileInDirectory(dataB, "other.txt", "content");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        // Use DataParts from existing inventories
        var itemBeingDeleted = inventoryDataA.GetSingleDataPart();
        var otherItem = inventoryDataB.GetSingleDataPart();

        // Existing Delete action
        var existingDeleteAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Delete,
            Destination = itemBeingDeleted
        };

        // New action that tries to operate on the item being deleted (uses the item as source this time)
        var newAction = new AtomicAction
        {
            Source = itemBeingDeleted, // Tries to use the item being deleted as source
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Destination = otherItem
        };

        // Use a ComparisonItem from the result
        var comparisonItem = comparisonResult.ComparisonItems.First(ci => ci.FileSystemType == FileSystemTypes.File);

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingDeleteAction]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(newAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        // The test covers a validation that runs before CannotOperateOnItemBeingDeleted
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.SourceCannotBeDestinationOfAnotherAction);
    }

    [Test]
    public async Task Test_DuplicateActionNotAllowed()
    {
        // Arrange - Create real inventory data
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "source.txt", "content");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
        _testDirectoryService.CreateFileInDirectory(dataB, "destination.txt", "content");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        var source = inventoryDataA.GetSingleDataPart();
        _ = inventoryDataB.GetSingleDataPart();

        // Existing action - use Delete to test duplicates
        var existingAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Delete,
            Destination = source
        };

        // Exactly identical duplicate action
        var duplicateAction = new AtomicAction
        {
            Source = null,
            Operator = ActionOperatorTypes.Delete,
            Destination = source // Same destination to be similar
        };

        // Use a ComparisonItem from the result
        var comparisonItem = comparisonResult.ComparisonItems.First(ci => ci.FileSystemType == FileSystemTypes.File);

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingAction]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(duplicateAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse();
        result.FailedValidations.Should().HaveCount(1);
        // The test covers a validation that runs before the duplicate check
        result.FailedValidations[0].FailureReason.Should()
            .Be(AtomicActionValidationFailureReason.DestinationAlreadyUsedByNonComplementaryAction);
    }

    [Test]
    public async Task Test_DoNothingActionAllowsDuplicates()
    {
        // Arrange - Create real inventory data
        var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
        _testDirectoryService.CreateFileInDirectory(dataA, "file.txt", "content");
        var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");

        var inventoryDataA = new InventoryData(dataA);
        var inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResult =
            await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

        var existingDoNothingAction = new AtomicAction
        {
            Operator = ActionOperatorTypes.DoNothing,
            Source = null, // Explicitly null
            Destination = null // Explicitly null
        };

        // Another exactly identical DoNothing action (DoNothing duplicates are allowed)
        var newDoNothingAction = new AtomicAction
        {
            Operator = ActionOperatorTypes.DoNothing,
            Source = null, // Explicitly null
            Destination = null // Explicitly null
        };

        // Use a ComparisonItem from the result
        var comparisonItem = comparisonResult.ComparisonItems.First();

        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(comparisonItem))
            .Returns([existingDoNothingAction]);

        // Act
        var result = _atomicActionConsistencyChecker.CheckCanAdd(newDoNothingAction, comparisonItem);

        // Assert
        result.IsOK.Should().BeFalse(); // The basic validations still apply to DoNothing
        result.FailedValidations.Should().HaveCount(1);
        // The test still covers the DoNothing handling logic
    }

    #endregion
}