using Autofac;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Helpers;
using ByteSync.Factories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;
using ByteSync.Models.FileSystems;
using ByteSync.Services.Sessions;
using ByteSync.Services.Synchronizations;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;
using NUnit.Framework.Legacy;

namespace ByteSync.Client.IntegrationTests.Services.Synchronizations;

public class TestSynchronizationActionHandler : IntegrationTest
{
    private ByteSyncEndpoint _currentEndPoint;
    private SynchronizationActionHandler _synchronizationActionHandler;
    
    [SetUp]
    public void SetUp()
    {
        RegisterType<DeltaManager, IDeltaManager>();
        RegisterType<CloudSessionLocalDataManager, ICloudSessionLocalDataManager>();
        RegisterType<TemporaryFileManagerFactory, ITemporaryFileManagerFactory>();
        RegisterType<TemporaryFileManager, ITemporaryFileManager>();
        RegisterType<DatesSetter, IDatesSetter>();
        RegisterType<ComparisonResultPreparer>();
        RegisterType<SynchronizationActionHandler>();
        BuildMoqContainer();

        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        _currentEndPoint = contextHelper.GenerateCurrentEndpoint();
        var testDirectory = _testDirectoryService.CreateTestDirectory();

        var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironmentService.Setup(m => m.AssemblyFullName).Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));

        var mockLocalApplicationDataManager = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalApplicationDataManager.Setup(m => m.ApplicationDataPath).Returns(IOUtils.Combine(testDirectory.FullName, 
            "ApplicationDataPath"));

        _synchronizationActionHandler = Container.Resolve<SynchronizationActionHandler>();
    }
    
    [Test]
    public void Test_FailUnknownOperator()
    {
        CloudSession cloudSession = new CloudSession
        {
            SessionId = "CloudSession1",
        };

        SharedActionsGroup sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_Test",
        };

        ClassicAssert.ThrowsAsync<ApplicationException>(() => _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup));
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    [TestCase(true, "\\subDir1\\subDir2\\")]
    [TestCase(false, "\\subDir1\\subDir2\\")]
    public async Task Test_SynchronizeContentOnly_Full_LocalTarget(bool isLocalSession, string? fileParentPathComplement = null)
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        
        var sourceA = sourceRoot.CreateSubdirectory("A");
        var sourceB = sourceRoot.CreateSubdirectory("B");

        var fileADirectory = sourceA;
        if (fileParentPathComplement.IsNotEmpty())
        {
            fileADirectory = new DirectoryInfo(sourceA.Combine(fileParentPathComplement!));
        }

        var fileA = _testDirectoryService.CreateFileInDirectory(fileADirectory, "fileToCopy1.txt", "fileToCopy1_content");
        fileA.LastWriteTimeUtc = DateTime.UtcNow.AddHours(-8);

        var fileB = new FileInfo(sourceB.Combine(fileParentPathComplement + "fileToCopy1.txt"));
        ClassicAssert.IsFalse(fileB.Exists);
        
        InventoryData inventoryDataA = new InventoryData(sourceA);
        InventoryData inventoryDataB = new InventoryData(sourceB);
        
        SessionSettings sessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        ComparisonResultPreparer comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        var comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);

        SharedDataPart source = new SharedDataPart("fileToCopy1.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "A", sourceA.FullName, fileParentPathComplement + "/fileToCopy1.txt", null, null, false);
        
        SharedDataPart target = new SharedDataPart("fileToCopy1.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, fileParentPathComplement + "/fileToCopy1.txt", null, null, false);
        
        SharedActionsGroup sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_Test",
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Targets = new HashSet<SharedDataPart> {target},
            Source = source,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToCopy1.txt", "fileToCopy1.txt", "/fileToCopy1.txt")
        };

        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
        
        fileA.Refresh();
        fileB.Refresh();
        
        fileA.Exists.Should().BeTrue();
        fileB.Exists.Should().BeTrue();
        
        var contentB = await File.ReadAllTextAsync(fileB.FullName);
        contentB.Should().Be("fileToCopy1_content");
        
        fileB.LastWriteTimeUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    [TestCase(true, "\\subDir1\\subDir2\\")]
    [TestCase(false, "\\subDir1\\subDir2\\")]
    public async Task Test_SynchronizeContentOnly_Delta_LocalTarget(bool isLocalSession, string? fileParentPathComplement = null)
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        
        var sourceA = sourceRoot.CreateSubdirectory("A");
        var sourceB = sourceRoot.CreateSubdirectory("B");

        var fileADirectory = sourceA;
        if (fileParentPathComplement.IsNotEmpty())
        {
            fileADirectory = new DirectoryInfo(sourceA.Combine(fileParentPathComplement!));
        }
        var fileA = _testDirectoryService.CreateFileInDirectory(fileADirectory, "fileToCopy1.txt", "fileToCopy1_versionA_content");
        fileA.LastWriteTimeUtc = DateTime.UtcNow.AddHours(-8);

        var fileBDirectory = sourceB;
        if (fileParentPathComplement.IsNotEmpty())
        {
            fileBDirectory = new DirectoryInfo(sourceB.Combine(fileParentPathComplement!));
        }
        var fileB = _testDirectoryService.CreateFileInDirectory(fileBDirectory, "fileToCopy1.txt", "fileToCopy1_versionB_content");

        InventoryData inventoryDataA = new InventoryData(sourceA);
        InventoryData inventoryDataB = new InventoryData(sourceB);
        
        SessionSettings sessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        ComparisonResultPreparer comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        var comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);

        var contentIdentityA = comparisonResult
            .ComparisonItems.Single(ci => ci.FileSystemType == FileSystemTypes.File)
            .ContentIdentities.Single(ci => ci.IsPresentIn(inventoryDataA.Inventory));
        
        var fileDescriptionA = contentIdentityA
            .FileSystemDescriptions.Cast<FileDescription>().Single();
        
        var signatureGuidA = fileDescriptionA.SignatureGuid;
        var signatureHashA = contentIdentityA.Core!.SignatureHash;
        
        var contentIdentityB = comparisonResult
            .ComparisonItems.Single(ci => ci.FileSystemType == FileSystemTypes.File)
            .ContentIdentities.Single(ci => ci.IsPresentIn(inventoryDataB.Inventory));
        
        var fileDescriptionB = contentIdentityB
            .FileSystemDescriptions.Cast<FileDescription>().Single();
        
        var signatureGuidB = fileDescriptionB.SignatureGuid;
        var signatureHashB = contentIdentityB.Core!.SignatureHash;
        
        SharedDataPart source = new SharedDataPart("fileToCopy1.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "A", sourceA.FullName, fileParentPathComplement + "fileToCopy1.txt", signatureGuidA, signatureHashA, false);
        
        SharedDataPart target = new SharedDataPart("fileToCopy1.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, fileParentPathComplement + "fileToCopy1.txt", signatureGuidB, signatureHashB, false);
        
        SharedActionsGroup sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_Test",
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Targets = [target],
            Source = source,
            SynchronizationType = SynchronizationTypes.Delta,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToCopy1.txt", "fileToCopy1.txt", "/fileToCopy1.txt")
        };
        
        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);

        fileA.Refresh();
        fileB.Refresh();
        
        fileA.Exists.Should().BeTrue();
        fileB.Exists.Should().BeTrue();
        
        var contentB = await File.ReadAllTextAsync(fileB.FullName);
        contentB.Should().Be("fileToCopy1_versionA_content");
    }
}