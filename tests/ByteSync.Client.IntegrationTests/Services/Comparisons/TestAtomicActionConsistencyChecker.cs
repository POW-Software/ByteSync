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
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using FluentAssertions;
using Moq;
using NUnit.Framework.Legacy;

namespace ByteSync.Client.IntegrationTests.Services.Comparisons;

public class TestAtomicActionConsistencyChecker : IntegrationTest
{
    private ComparisonResultPreparer _comparisonResultPreparer;
    private AtomicActionConsistencyChecker _atomicActionConsistencyChecker;

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
        mockEnvironmentService.Setup(m => m.AssemblyFullName).Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));

        var mockLocalApplicationDataManager = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalApplicationDataManager.Setup(m => m.ApplicationDataPath).Returns(IOUtils.Combine(testDirectory.FullName, 
            "ApplicationDataPath"));
        
        var atomicActionRepository = Container.Resolve<Mock<IAtomicActionRepository>>();
        atomicActionRepository.Setup(m => m.GetAtomicActions(It.IsAny<ComparisonItem>())).Returns(new List<AtomicAction>());

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

        InventoryData inventoryDataA = new InventoryData(dataA);
        InventoryData inventoryDataB = new InventoryData(dataB);

        var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        
        comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);
        
        atomicAction = new AtomicAction
        {
            Operator = ActionOperatorTypes.Create,
            Destination = inventoryDataB.GetSingleDataPart(),
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

       InventoryData inventoryDataA = new InventoryData(dataA);
       InventoryData inventoryDataB = new InventoryData(dataB);

       var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
       
       comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

       atomicAction = new AtomicAction
       {
           Operator = ActionOperatorTypes.Create,
           Destination = inventoryDataA.GetSingleDataPart(), // On demande la création sur A, ça ne peut passer
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
    public async Task Test_DirectoryOnlyOnA_3(DataTypes dataType, LinkingKeys linkingKey, int comparisonItemViewModelsCount)
    {
      // Dans ce test, inventoryDataB est de type File
      // On ne peut donc pas copier Dir1 vers B

      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      dataA.CreateSubdirectory("Dir1");
      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      var fileB = _testDirectoryService.CreateFileInDirectory(dataB, "fileB.txt", "fileBContent___");

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(fileB);

      var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings(dataType, linkingKey);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      // List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      // ClassicAssert.AreEqual(comparisonItemViewModelsCount, comparisonItemViewModels.Count);

      atomicAction = new AtomicAction
      {
          Operator = ActionOperatorTypes.Create,
          Destination = inventoryDataB.GetSingleDataPart(),
      };

      
      var checkResult = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonResult.ComparisonItems);
      
      checkResult.IsOK.Should().BeFalse();
        checkResult.ValidComparisons.Count.Should().Be(0);
        checkResult.NonValidComparisons.Count.Should().Be(comparisonItemViewModelsCount);
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
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var fileA = _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "fileAContent");
      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      var fileB = _testDirectoryService.CreateFileInDirectory(dataB, "fileB.txt", "fileBContent___");

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(fileB);

      var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings();

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      atomicAction = new AtomicAction
      {
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetSingleDataPart(), // On demande la création sur B, ça ne peut passer
      };

      var checkResult = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonResult.ComparisonItems);
      // ClassicAssert.IsFalse(checkResult.IsOK);
      // ClassicAssert.AreEqual(0, checkResult.ValidComparisons.Count);
      // ClassicAssert.AreEqual(2, checkResult.NonValidComparisons.Count);
      
      checkResult.IsOK.Should().BeFalse();
        checkResult.ValidComparisons.Count.Should().Be(0);
        checkResult.NonValidComparisons.Count.Should().Be(2);
  }

  [Test]
  [TestCase(ActionOperatorTypes.SynchronizeContentAndDate, 0)]
  [TestCase(ActionOperatorTypes.SynchronizeContentOnly, 0)]
  [TestCase(ActionOperatorTypes.SynchronizeDate, 0)]
  public async Task Test_FileOnAAndB_SameContentAndDate(ActionOperatorTypes actionOperator, int expectedValidItems)
  {
      // Même fichier sur A et B, on ne doit pas pouvoir Synchroniser contenu, ni synchroniser date, ni synchroniser contenu et date

      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var fileA = _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "fileAContent");
      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      fileA.CopyTo(dataB.Combine(fileA.Name));

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB);

      var cloudSessionSettings = SessionSettingsGenerator.GenerateSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      // List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      // ClassicAssert.AreEqual(1, comparisonItemViewModels.Count);

      atomicAction = new AtomicAction
      {
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = actionOperator,
          Destination = inventoryDataB.GetSingleDataPart(), // On demande la création sur B, ça ne peut passer
      };

      var checkResult = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonResult.ComparisonItems);
      if (expectedValidItems == 0)
      {
          // ClassicAssert.IsFalse(checkResult.IsOK);
          checkResult.IsOK.Should().BeFalse();
      }
      else
      {
          // ClassicAssert.IsTrue(checkResult.IsOK);
            checkResult.IsOK.Should().BeTrue();
      }
      // ClassicAssert.AreEqual(expectedValidItems, checkResult.ValidComparisons.Count);
      // ClassicAssert.AreEqual(1- expectedValidItems, checkResult.NonValidComparisons.Count);
      
        checkResult.ValidComparisons.Count.Should().Be(expectedValidItems);
        checkResult.NonValidComparisons.Count.Should().Be(1 - expectedValidItems);
  }

  /*
  [Test]
  [TestCase(ActionOperatorTypes.SynchronizeContentAndDate, 1)]
  [TestCase(ActionOperatorTypes.SynchronizeContentOnly, 0)]
  [TestCase(ActionOperatorTypes.SynchronizeDate, 1)]
  public async Task Test_FileOnAAndB_SameContent_DifferentDate(ActionOperatorTypes actionOperator, int expectedValidItems)
  {
      // Même fichier sur A et B, on ne doit pas pouvoir Synchroniser contenu, ni synchroniser date, ni synchroniser contenu et date

      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var fileA = _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "fileAContent");
      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      fileA.CopyTo(dataB.Combine(fileA.Name));
      fileA.LastWriteTimeUtc = fileA.LastWriteTimeUtc.AddHours(-2);

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      ClassicAssert.AreEqual(1, comparisonItemViewModels.Count);

      atomicAction = new AtomicAction
      {
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = actionOperator,
          Destination = inventoryDataB.GetSingleDataPart(), // On demande la création sur B, ça ne peut passer
      };
      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      if (expectedValidItems == 1)
      {
          ClassicAssert.IsTrue(checkResult.IsOK);
      }
      else
      {
          ClassicAssert.IsFalse(checkResult.IsOK);
      }
      ClassicAssert.AreEqual(expectedValidItems, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(1 - expectedValidItems, checkResult.NonValidComparisons.Count);
  }

  [Test]
  public async Task Test_FileOnAAndB_Delete()
  {
      // todo 050523
      throw new Exception("Review implementation!");


      AtomicAction atomicAction;
      AtomicCondition atomicCondition;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var fileAA = _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "fileAContent");
      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      var fileBA = _testDirectoryService.CreateFileInDirectory(dataB, "fileA.txt", "fileBContent___");

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);

      // Si A existe sur B, alors supprimer sur B
      SynchronizationRule synchronizationRule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
      atomicCondition = new AtomicCondition
      {
          Source = inventoryDataA.GetSingleDataPart(),
          ComparisonElement = ComparisonElement.Presence,
          ConditionOperator = ConditionOperatorTypes.ExistsOn,
          Destination = inventoryDataB.GetSingleDataPart(),
      };
      synchronizationRule.Conditions.Add(atomicCondition);
      atomicAction = new AtomicAction
      {
          Operator = ActionOperatorTypes.Delete,
          Destination = inventoryDataB.GetSingleDataPart(),
      };
      synchronizationRule.AddAction(atomicAction);


      var synchronizationRuleMatcher = new SynchronizationRuleMatcher(_mockObjectsGenerator.SessionDataHolder.Object,
          new AtomicActionConsistencyChecker());
      synchronizationRuleMatcher.MakeMatches(comparisonItemViewModels, new List<SynchronizationRule> {synchronizationRule});
      ClassicAssert.AreEqual(1, comparisonItemViewModels.Single().SynchronizationActions.Count);

      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();

      // Action manuelle : supprimer sur A : OK
      atomicAction = new AtomicAction
      {
          Operator = ActionOperatorTypes.Delete,
          Destination = inventoryDataA.GetSingleDataPart(),
      };
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsTrue(checkResult.IsOK);

      // Action manuelle : supprimer sur B
      atomicAction = new AtomicAction
      {
          Operator = ActionOperatorTypes.Delete,
          Destination = inventoryDataB.GetSingleDataPart(),
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsTrue(checkResult.IsOK);
  }

  [Test]
  public async Task Test_FileOnAAndB_DifferentContent_A()
  {
      AtomicAction atomicAction;
      AtomicCondition atomicCondition;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var fileAA = _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "fileAContent");
      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      var fileBA = _testDirectoryService.CreateFileInDirectory(dataB, "fileA.txt", "fileBContent___");

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);

      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();

      // Action manuelle : Copier de A vers B : Succès
      atomicAction = new AtomicAction
      {
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetSingleDataPart(),
      };
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsTrue(checkResult.IsOK);

      // Action manuelle : Copier de B vers A : Succès
      atomicAction = new AtomicAction
      {
          Source = inventoryDataB.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataA.GetSingleDataPart(),
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsTrue(checkResult.IsOK);
  }

  // [Test]
  // public async Task Test_FileOnAAndB_DifferentContent_B()
  // {
  //     // todo 050523
  //     throw new Exception("Review implementation!");
  //
  //     AtomicAction atomicAction;
  //     AtomicCondition atomicCondition;
  //     AtomicActionConsistencyChecker atomicActionConsistencyChecker;
  //
  //     ComparisonResult comparisonResult;
  //
  //     var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
  //     var fileAA = _testDirectoryService.CreateFileInDirectory(dataA, "fileA.txt", "fileAContent");
  //     var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
  //     var fileBA = _testDirectoryService.CreateFileInDirectory(dataB, "fileA.txt", "fileBContent___");
  //
  //     InventoryData inventoryDataA = new InventoryData(dataA);
  //     InventoryData inventoryDataB = new InventoryData(dataB);
  //
  //     var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
  //         DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);
  //
  //     comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);
  //
  //     List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
  //
  //     // Si A existe sur B, alors supprimer sur B
  //     SynchronizationRule synchronizationRule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
  //     atomicCondition = new AtomicCondition
  //     {
  //         Source = inventoryDataA.GetSingleDataPart(),
  //         ComparisonElement = ComparisonElement.Presence,
  //         ConditionOperator = ConditionOperatorTypes.ExistsOn,
  //         Destination = inventoryDataB.GetSingleDataPart(),
  //     };
  //     synchronizationRule.Conditions.Add(atomicCondition);
  //     atomicAction = new AtomicAction
  //     {
  //         Source = inventoryDataA.GetSingleDataPart(),
  //         Operator = ActionOperatorTypes.SynchronizeContentAndDate,
  //         Destination = inventoryDataB.GetSingleDataPart(),
  //     };
  //     synchronizationRule.AddAction(atomicAction);
  //
  //
  //     var synchronizationRuleMatcher = new SynchronizationRuleMatcher(_mockObjectsGenerator.SessionDataHolder.Object, new AtomicActionConsistencyChecker());
  //     synchronizationRuleMatcher.MakeMatches(comparisonItemViewModels, new List<SynchronizationRule> {synchronizationRule});
  //     ClassicAssert.AreEqual(1, comparisonItemViewModels.Single().SynchronizationActions.Count);
  //
  //     atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();
  //
  //     // Action manuelle : supprimer sur A
  //     atomicAction = new AtomicAction
  //     {
  //         Operator = ActionOperatorTypes.Delete,
  //         Destination = inventoryDataA.GetSingleDataPart(),
  //     };
  //     var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
  //     ClassicAssert.IsTrue(checkResult.IsOK);
  //
  //     // Action manuelle : supprimer sur B
  //     atomicAction = new AtomicAction
  //     {
  //         Operator = ActionOperatorTypes.Delete,
  //         Destination = inventoryDataB.GetSingleDataPart(),
  //     };
  //     checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
  //     ClassicAssert.IsTrue(checkResult.IsOK);
  //
  //     // Action manuelle : Copier de A vers B : Échec car la règle auto le fait déjà
  //     atomicAction = new AtomicAction
  //     {
  //         Source = inventoryDataA.GetSingleDataPart(),
  //         Operator = ActionOperatorTypes.SynchronizeContentAndDate,
  //         Destination = inventoryDataB.GetSingleDataPart(),
  //     };
  //     checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
  //     ClassicAssert.IsTrue(checkResult.IsOK);
  //
  //     // Action manuelle : Copier de B vers A
  //     atomicAction = new AtomicAction
  //     {
  //         Source = inventoryDataB.GetSingleDataPart(),
  //         Operator = ActionOperatorTypes.SynchronizeContentAndDate,
  //         Destination = inventoryDataA.GetSingleDataPart(),
  //     };
  //     checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
  //     ClassicAssert.IsTrue(checkResult.IsOK);
  // }

  [Test]
  public async Task Test_AnalysisError_1()
  {
      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2ContentOnA");
      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      var file2B = _testDirectoryService.CreateFileInDirectory(dataB, "file2.txt", "file2ContentOnB");
      // On empêche l'analyse sur file2 de dataB
      var blockingStream = new FileStream(file2B.FullName, FileMode.Open, FileAccess.Read, FileShare.None);

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      ClassicAssert.AreEqual(1, comparisonItemViewModels.Count);
      foreach (var comparisonItemViewModel in comparisonItemViewModels)
      {
          ClassicAssert.AreEqual(0, comparisonItemViewModel.TD_SynchronizationActions.Count);
      }

      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();

      // A vers B
      atomicAction = new AtomicAction
      {
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetSingleDataPart(),
      };
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsFalse(checkResult.IsOK);

      // B vers A
      atomicAction = new AtomicAction
      {
          Source = inventoryDataB.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataA.GetSingleDataPart(),
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsFalse(checkResult.IsOK);
  }

  [Test]
  public async Task Test_AnalysisError_MultiTarget_1()
  {
      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2ContentOnA");

      var dataB1 = _testDirectoryService.CreateSubTestDirectory("dataB1");
      var file2B1 = _testDirectoryService.CreateFileInDirectory(dataB1, "file2.txt", "file2ContentOnB");
      // On empêche l'analyse sur file2 de dataB
      var blockingStream = new FileStream(file2B1.FullName, FileMode.Open, FileAccess.Read, FileShare.None);

      var dataB2 = _testDirectoryService.CreateSubTestDirectory("dataB2");
      var file2B2 = _testDirectoryService.CreateFileInDirectory(dataB2, "file2.txt", "file2ContentOnB");

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB1, dataB2);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Checksum);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      ClassicAssert.AreEqual(1, comparisonItemViewModels.Count);
      foreach (var comparisonItemViewModel in comparisonItemViewModels)
      {
          ClassicAssert.AreEqual(0, comparisonItemViewModel.TD_SynchronizationActions.Count);
      }

      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();

      // A vers B1
      atomicAction = new AtomicAction
      {
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetDataPart("dataB1"),
      };
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsFalse(checkResult.IsOK);

      // A vers B2
      atomicAction = new AtomicAction
      {
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetDataPart("dataB2"),
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsTrue(checkResult.IsOK);

      // B1 vers A
      atomicAction = new AtomicAction
      {
          Source = inventoryDataB.GetDataPart("dataB1"),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataA.GetSingleDataPart(),
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsFalse(checkResult.IsOK);

      // B2 vers A
      atomicAction = new AtomicAction
      {
          Source = inventoryDataB.GetDataPart("dataB2"),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataA.GetSingleDataPart(),
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsTrue(checkResult.IsOK);
  }

  [Test]
  public async Task Test_AnalysisError_MultiTarget_2()
  {
      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2ContentOnA");
      var blockingStream = new FileStream(file2A.FullName, FileMode.Open, FileAccess.Read, FileShare.None);

      var dataB1 = _testDirectoryService.CreateSubTestDirectory("dataB1");
      var file2B1 = _testDirectoryService.CreateFileInDirectory(dataB1, "file2.txt", "file2ContentOnB");

      var dataB2 = _testDirectoryService.CreateSubTestDirectory("dataB2");
      var file2B2 = _testDirectoryService.CreateFileInDirectory(dataB2, "file2.txt", "file2ContentOnB");

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB1, dataB2);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Checksum);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      ClassicAssert.AreEqual(1, comparisonItemViewModels.Count);
      foreach (var comparisonItemViewModel in comparisonItemViewModels)
      {
          ClassicAssert.AreEqual(0, comparisonItemViewModel.TD_SynchronizationActions.Count);
      }

      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();

      // A vers B1
      atomicAction = new AtomicAction
      {
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetDataPart("dataB1"),
      };
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsFalse(checkResult.IsOK);

      // A vers B1
      atomicAction = new AtomicAction
      {
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetDataPart("dataB2"),
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsFalse(checkResult.IsOK);

      // B1 vers A
      atomicAction = new AtomicAction
      {
          Source = inventoryDataB.GetDataPart("dataB1"),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataA.GetSingleDataPart(),
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsFalse(checkResult.IsOK);

      // B2 vers A
      atomicAction = new AtomicAction
      {
          Source = inventoryDataB.GetDataPart("dataB2"),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataA.GetSingleDataPart(),
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
      ClassicAssert.IsFalse(checkResult.IsOK);
  }

  // [Test]
  // public async Task Test_SynchronizationRule_Then_TargetedAction()
  // {
  //     // todo 050523
  //     throw new Exception("Review implementation!");
  //
  //     AtomicAction atomicAction;
  //     AtomicCondition atomicCondition;
  //     SynchronizationRuleMatcher synchronizationRuleMatcher;
  //     SynchronizationRule synchronizationRule;
  //     AtomicActionConsistencyChecker atomicActionConsistencyChecker;
  //
  //     ComparisonResult comparisonResult;
  //
  //     var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
  //     var file1 = _testDirectoryService.CreateFileInDirectory(dataA, "file1.txt", "file1Content");
  //     var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2Content");
  //     var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
  //     var file2B = _testDirectoryService.CreateFileInDirectory(dataB, "file2.txt", "file2ContentB");
  //
  //     InventoryData inventoryDataA = new InventoryData(dataA);
  //     InventoryData inventoryDataB = new InventoryData(dataB);
  //
  //     var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
  //         DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);
  //
  //     comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);
  //
  //     List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
  //     ClassicAssert.AreEqual(2, comparisonItemViewModels.Count);
  //     foreach (var comparisonItemViewModel in comparisonItemViewModels)
  //     {
  //         ClassicAssert.AreEqual(0, comparisonItemViewModel.SynchronizationActions.Count);
  //     }
  //
  //     // On ajoute une règle de synchro qui entrâine la même action
  //     synchronizationRule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
  //     atomicCondition = new AtomicCondition(
  //         inventoryDataA.GetSingleDataPart(), ComparisonElement.Presence,
  //         ConditionOperatorTypes.ExistsOn, inventoryDataB.GetSingleDataPart());
  //     synchronizationRule.Conditions.Add(atomicCondition);
  //     atomicAction = new AtomicAction
  //     {
  //         Operator = ActionOperatorTypes.Delete,
  //         Destination = inventoryDataB.GetSingleDataPart(),
  //     };
  //     synchronizationRule.AddAction(atomicAction);
  //
  //     synchronizationRuleMatcher = new SynchronizationRuleMatcher(_mockObjectsGenerator.SessionDataHolder.Object, new AtomicActionConsistencyChecker());
  //     synchronizationRuleMatcher.MakeMatches(comparisonItemViewModels, new List<SynchronizationRule> {synchronizationRule});
  //
  //     // La règle ne s'est pas ajoutée
  //     ClassicAssert.AreEqual(1, comparisonItemViewModels.Single(civm => civm.PathIdentity.FileName.Contains("file2.txt"))
  //         .SynchronizationActions.Count);
  //     ClassicAssert.AreEqual(1, comparisonItemViewModels.Single(civm => civm.PathIdentity.FileName.Contains("file2.txt"))
  //         .SynchronizationActions.Count(sa => sa.IsFromSynchronizationRule));
  //
  //
  //     atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();
  //
  //     atomicAction = new AtomicAction
  //     {
  //         AtomicActionId = "Targeted",
  //         Operator = ActionOperatorTypes.Delete,
  //         Destination = inventoryDataB.GetSingleDataPart(),
  //         SynchronizationRule = null,
  //     };
  //     var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
  //     ClassicAssert.AreEqual(1, checkResult.ValidComparisonItemViews.Count);
  //     ClassicAssert.AreEqual(1, checkResult.NonValidComparisonItemViews.Count);
  //
  //
  //     atomicAction = new AtomicAction
  //     {
  //         AtomicActionId = "Targeted2",
  //         Source = inventoryDataA.GetSingleDataPart(),
  //         Operator = ActionOperatorTypes.SynchronizeContentAndDate,
  //         Destination = inventoryDataB.GetSingleDataPart(),
  //         SynchronizationRule = null,
  //     };
  //     checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
  //     ClassicAssert.AreEqual(2, checkResult.ValidComparisonItemViews.Count);
  //     ClassicAssert.AreEqual(0, checkResult.NonValidComparisonItemViews.Count);
  // }

  // [Test]
  // public async Task Test_SynchronizationRule_Then_TargetedAction_DoNothing()
  // {
  //     // todo 050523
  //     throw new Exception("Review implementation!");
  //
  //     AtomicAction atomicAction;
  //     AtomicCondition atomicCondition;
  //     SynchronizationRuleMatcher synchronizationRuleMatcher;
  //     SynchronizationRule synchronizationRule;
  //     AtomicActionConsistencyChecker atomicActionConsistencyChecker;
  //
  //     ComparisonResult comparisonResult;
  //
  //     var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
  //     var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2Content");
  //     var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
  //     var file2B = _testDirectoryService.CreateFileInDirectory(dataB, "file2.txt", "file2ContentB");
  //
  //     InventoryData inventoryDataA = new InventoryData(dataA);
  //     InventoryData inventoryDataB = new InventoryData(dataB);
  //
  //     var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
  //         DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);
  //
  //     comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);
  //
  //     List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
  //     ClassicAssert.AreEqual(1, comparisonItemViewModels.Count);
  //     ClassicAssert.AreEqual(0, comparisonItemViewModels.SelectMany(civm => civm.SynchronizationActions).Count());
  //     var comparisonItemViewModel = comparisonItemViewModels.Single();
  //
  //     synchronizationRule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
  //     atomicCondition = new AtomicCondition(
  //         inventoryDataA.GetSingleDataPart(), ComparisonElement.Presence,
  //         ConditionOperatorTypes.ExistsOn, inventoryDataB.GetSingleDataPart());
  //     synchronizationRule.Conditions.Add(atomicCondition);
  //     atomicAction = new AtomicAction
  //     {
  //         Operator = ActionOperatorTypes.Delete,
  //         Destination = inventoryDataB.GetSingleDataPart(),
  //     };
  //     synchronizationRule.AddAction(atomicAction);
  //
  //     synchronizationRuleMatcher = new SynchronizationRuleMatcher(_mockObjectsGenerator.SessionDataHolder.Object, new AtomicActionConsistencyChecker());
  //     synchronizationRuleMatcher.MakeMatches(comparisonItemViewModels, new List<SynchronizationRule> {synchronizationRule});
  //
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count);
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count(sa => sa.IsFromSynchronizationRule));
  //
  //     atomicAction = new AtomicAction
  //     {
  //         AtomicActionId = "Targeted",
  //         Operator = ActionOperatorTypes.DoNothing,
  //         SynchronizationRule = null,
  //     };
  //     atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();
  //     var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModels);
  //     ClassicAssert.AreEqual(1, checkResult.ValidComparisonItemViews.Count);
  //     ClassicAssert.AreEqual(0, checkResult.NonValidComparisonItemViews.Count);
  //
  //
  //     _mockObjectsGenerator.SetSynchronizationRules(synchronizationRule);
  //     ComparisonItemActionsManager comparisonItemActionsManager = new ComparisonItemActionsManager();
  //     comparisonItemActionsManager.AddTargetedAction(atomicAction, comparisonItemViewModels.Single());
  //
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count);
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count(sa => sa.IsTargeted));
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count(sa => sa.AtomicAction.IsDoNothing));
  // }

  // [Test]
  // public async Task Test_TargetedAction_DoNothing_Then_SynchronizationRule()
  // {
  //     // todo 050523
  //     throw new Exception("Review implementation!");
  //
  //     AtomicAction atomicAction;
  //     AtomicCondition atomicCondition;
  //     SynchronizationRuleMatcher synchronizationRuleMatcher;
  //     SynchronizationRule synchronizationRule;
  //     AtomicActionConsistencyChecker atomicActionConsistencyChecker;
  //     ComparisonItemActionsManager comparisonItemActionsManager;
  //
  //     ComparisonResult comparisonResult;
  //
  //     var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
  //     var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2Content");
  //     var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
  //     var file2B = _testDirectoryService.CreateFileInDirectory(dataB, "file2.txt", "file2ContentB");
  //
  //     InventoryData inventoryDataA = new InventoryData(dataA);
  //     InventoryData inventoryDataB = new InventoryData(dataB);
  //
  //     var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
  //         DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);
  //
  //     comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);
  //
  //     List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
  //     ClassicAssert.AreEqual(1, comparisonItemViewModels.Count);
  //     ClassicAssert.AreEqual(0, comparisonItemViewModels.SelectMany(civm => civm.SynchronizationActions).Count());
  //     var comparisonItemViewModel = comparisonItemViewModels.Single();
  //
  //
  //     atomicAction = new AtomicAction
  //     {
  //         AtomicActionId = "Targeted",
  //         Operator = ActionOperatorTypes.DoNothing,
  //         SynchronizationRule = null,
  //     };
  //     atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();
  //     var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, comparisonItemViewModel);
  //     ClassicAssert.AreEqual(1, checkResult.ValidComparisonItemViews.Count);
  //     ClassicAssert.AreEqual(0, checkResult.NonValidComparisonItemViews.Count);
  //
  //     comparisonItemActionsManager = new ComparisonItemActionsManager();
  //     comparisonItemActionsManager.AddTargetedAction(atomicAction, comparisonItemViewModels.Single());
  //
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count);
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count(sa => sa.IsTargeted));
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count(sa => sa.AtomicAction.IsDoNothing));
  //
  //
  //     synchronizationRule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);
  //     atomicCondition = new AtomicCondition(
  //         inventoryDataA.GetSingleDataPart(), ComparisonElement.Presence,
  //         ConditionOperatorTypes.ExistsOn, inventoryDataB.GetSingleDataPart());
  //     synchronizationRule.Conditions.Add(atomicCondition);
  //     atomicAction = new AtomicAction
  //     {
  //         Operator = ActionOperatorTypes.Delete,
  //         Destination = inventoryDataB.GetSingleDataPart(),
  //     };
  //     synchronizationRule.AddAction(atomicAction);
  //
  //     _mockObjectsGenerator.SetSynchronizationRules(synchronizationRule);
  //
  //     synchronizationRuleMatcher = new SynchronizationRuleMatcher(_mockObjectsGenerator.SessionDataHolder.Object, new AtomicActionConsistencyChecker());
  //     synchronizationRuleMatcher.MakeMatches(comparisonItemViewModels);
  //
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count);
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count(sa => sa.IsTargeted));
  //     ClassicAssert.AreEqual(1, comparisonItemViewModel.SynchronizationActions.Count(sa => sa.AtomicAction.IsDoNothing));
  // }

  [Test]
  [TestCase(ActionOperatorTypes.SynchronizeContentOnly, ActionOperatorTypes.SynchronizeDate)]
  [TestCase(ActionOperatorTypes.SynchronizeDate, ActionOperatorTypes.SynchronizeContentOnly)]
  public async Task Test_CopyContentOnly_Then_CopyDate(ActionOperatorTypes operator1, ActionOperatorTypes operator2)
  {
      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var file1 = _testDirectoryService.CreateFileInDirectory(dataA, "file1.txt", "file1Content");
      var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2Content");
      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      var file2B = _testDirectoryService.CreateFileInDirectory(dataB, "file2.txt", "file2ContentB");

      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      ClassicAssert.AreEqual(2, comparisonItemViewModels.Count);
      foreach (var comparisonItemViewModel in comparisonItemViewModels)
      {
          ClassicAssert.AreEqual(0, comparisonItemViewModel.TD_SynchronizationActions.Count);
      }

      var file2ComparisonItemViewModel = comparisonItemViewModels.Single(civm => civm.PathIdentity.FileName.Equals("file2.txt"));

      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted1",
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = operator1,
          Destination = inventoryDataB.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(1, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(0, checkResult.NonValidComparisons.Count);

      _mockObjectsGenerator.SetSynchronizationRules();
      ComparisonItemActionsManager comparisonItemActionsManager = new ComparisonItemActionsManager();
      comparisonItemActionsManager.AddTargetedAction(atomicAction, file2ComparisonItemViewModel);

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted2",
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = operator2,
          Destination = inventoryDataB.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(1, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(0, checkResult.NonValidComparisons.Count);
  }

  [Test]
  public async Task Test_OppositeRules()
  {
      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var file1 = _testDirectoryService.CreateFileInDirectory(dataA, "file1.txt", "file1Content");
      var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2Content");
      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      var file2B = _testDirectoryService.CreateFileInDirectory(dataB, "file2.txt", "file2ContentB");


      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      ClassicAssert.AreEqual(2, comparisonItemViewModels.Count);
      foreach (var comparisonItemViewModel in comparisonItemViewModels)
      {
          ClassicAssert.AreEqual(0, comparisonItemViewModel.TD_SynchronizationActions.Count);
      }

      var file2ComparisonItemViewModel = comparisonItemViewModels.Single(civm => civm.PathIdentity.FileName.Equals("file2.txt"));

      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted1",
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(1, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(0, checkResult.NonValidComparisons.Count);

      _mockObjectsGenerator.SetSynchronizationRules();
      ComparisonItemActionsManager comparisonItemActionsManager = new ComparisonItemActionsManager();
      comparisonItemActionsManager.AddTargetedAction(atomicAction, file2ComparisonItemViewModel);

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted2",
          Source = inventoryDataB.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataA.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(0, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(1, checkResult.NonValidComparisons.Count);
  }

  [Test]
  public async Task Test_CyclingRules_1()
  {
      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var file1 = _testDirectoryService.CreateFileInDirectory(dataA, "file1.txt", "file1Content");
      var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2Content");

      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      var file2B = _testDirectoryService.CreateFileInDirectory(dataB, "file2.txt", "file2ContentB");

      var dataC = _testDirectoryService.CreateSubTestDirectory("dataC");
      var file2C = _testDirectoryService.CreateFileInDirectory(dataC, "file2.txt", "file2ContentC");


      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB);
      InventoryData inventoryDataC = new InventoryData(dataC);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB, inventoryDataC);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      ClassicAssert.AreEqual(2, comparisonItemViewModels.Count);
      foreach (var comparisonItemViewModel in comparisonItemViewModels)
      {
          ClassicAssert.AreEqual(0, comparisonItemViewModel.TD_SynchronizationActions.Count);
      }

      var file2ComparisonItemViewModel = comparisonItemViewModels.Single(civm => civm.PathIdentity.FileName.Equals("file2.txt"));

      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted1",
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(1, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(0, checkResult.NonValidComparisons.Count);

      _mockObjectsGenerator.SetSynchronizationRules();
      ComparisonItemActionsManager comparisonItemActionsManager = new ComparisonItemActionsManager();
      comparisonItemActionsManager.AddTargetedAction(atomicAction, file2ComparisonItemViewModel);

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted2",
          Source = inventoryDataB.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataC.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(0, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(1, checkResult.NonValidComparisons.Count);

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted3",
          Source = inventoryDataC.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataA.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(0, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(1, checkResult.NonValidComparisons.Count);
  }

  [Test]
  public async Task Test_CyclingRules_2()
  {
      AtomicAction atomicAction;
      AtomicActionConsistencyChecker atomicActionConsistencyChecker;

      ComparisonResult comparisonResult;

      var dataA = _testDirectoryService.CreateSubTestDirectory("dataA");
      var file1 = _testDirectoryService.CreateFileInDirectory(dataA, "file1.txt", "file1Content");
      var file2A = _testDirectoryService.CreateFileInDirectory(dataA, "file2.txt", "file2Content");

      var dataB = _testDirectoryService.CreateSubTestDirectory("dataB");
      var file2B = _testDirectoryService.CreateFileInDirectory(dataB, "file2.txt", "file2ContentB");

      var dataC = _testDirectoryService.CreateSubTestDirectory("dataC");
      var file2C = _testDirectoryService.CreateFileInDirectory(dataC, "file2.txt", "file2ContentC");


      InventoryData inventoryDataA = new InventoryData(dataA);
      InventoryData inventoryDataB = new InventoryData(dataB);
      InventoryData inventoryDataC = new InventoryData(dataC);

      var cloudSessionSettings = SessionSettingsHelper.BuildDefaultSessionSettings(
          DataTypes.FilesDirectories, LinkingKeys.RelativePath, AnalysisModes.Smart);

      comparisonResult = await _comparisonResultPreparer.BuildAndCompare(cloudSessionSettings, inventoryDataA, inventoryDataB, inventoryDataC);

      List<ComparisonItemViewModel> comparisonItemViewModels = _comparisonResultPreparer.BuildComparisonItemViewModels(comparisonResult);
      ClassicAssert.AreEqual(2, comparisonItemViewModels.Count);
      foreach (var comparisonItemViewModel in comparisonItemViewModels)
      {
          ClassicAssert.AreEqual(0, comparisonItemViewModel.TD_SynchronizationActions.Count);
      }

      var file2ComparisonItemViewModel = comparisonItemViewModels.Single(civm => civm.PathIdentity.FileName.Equals("file2.txt"));

      atomicActionConsistencyChecker = new AtomicActionConsistencyChecker();

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted1",
          Source = inventoryDataA.GetSingleDataPart(),
          Operator = ActionOperatorTypes.SynchronizeContentAndDate,
          Destination = inventoryDataB.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      var checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(1, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(0, checkResult.NonValidComparisons.Count);

      _mockObjectsGenerator.SetSynchronizationRules();
      ComparisonItemActionsManager comparisonItemActionsManager = new ComparisonItemActionsManager();
      comparisonItemActionsManager.AddTargetedAction(atomicAction, file2ComparisonItemViewModel);

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted2",
          Operator = ActionOperatorTypes.Delete,
          Destination = inventoryDataC.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(1, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(0, checkResult.NonValidComparisons.Count);

      comparisonItemActionsManager.AddTargetedAction(atomicAction, file2ComparisonItemViewModel);

      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted3",
          Operator = ActionOperatorTypes.Delete,
          Destination = inventoryDataB.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(0, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(1, checkResult.NonValidComparisons.Count);


      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted4",
          Operator = ActionOperatorTypes.Delete,
          Destination = inventoryDataA.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(0, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(1, checkResult.NonValidComparisons.Count);


      atomicAction = new AtomicAction
      {
          AtomicActionId = "Targeted4",
          Operator = ActionOperatorTypes.Delete,
          Destination = inventoryDataC.GetSingleDataPart(),
          SynchronizationRule = null,
      };
      checkResult = atomicActionConsistencyChecker.CheckCanAdd(atomicAction, file2ComparisonItemViewModel);
      ClassicAssert.AreEqual(0, checkResult.ValidComparisons.Count);
      ClassicAssert.AreEqual(1, checkResult.NonValidComparisons.Count);
  }*/
}