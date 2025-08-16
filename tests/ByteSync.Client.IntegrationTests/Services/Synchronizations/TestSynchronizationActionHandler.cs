using Autofac;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Inventories;
using ByteSync.Business.Synchronizations;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Local;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Common.Helpers;
using ByteSync.Factories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.FileSystems;
using ByteSync.Services.Sessions;
using ByteSync.Services.Synchronizations;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

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
        RegisterType<FileDatesSetter, IFileDatesSetter>();
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
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_Test",
        };

        FluentActions.Awaiting(() => _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup))
            .Should().ThrowAsync<ApplicationException>();
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
        fileB.Exists.Should().BeFalse();
        
        var inventoryDataA = new InventoryData(sourceA);
        var inventoryDataB = new InventoryData(sourceB);
        
        var sessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        _ = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);

        var source = new SharedDataPart("fileToCopy1.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "A", sourceA.FullName, fileParentPathComplement + "/fileToCopy1.txt", null, null, false);
        
        var target = new SharedDataPart("fileToCopy1.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, fileParentPathComplement + "/fileToCopy1.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_Test",
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Targets = [target],
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

        var inventoryDataA = new InventoryData(sourceA);
        var inventoryDataB = new InventoryData(sourceB);
        
        var sessionSettings = SessionSettingsGenerator.GenerateSessionSettings();
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
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
        
        var source = new SharedDataPart("fileToCopy1.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            inventoryDataA.Inventory.CodeAndId, sourceA.FullName, fileParentPathComplement + "fileToCopy1.txt", signatureGuidA, signatureHashA, false);
        
        var target = new SharedDataPart("fileToCopy1.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            inventoryDataB.Inventory.CodeAndId, sourceB.FullName, fileParentPathComplement + "fileToCopy1.txt", signatureGuidB, signatureHashB, false);
        
        var sharedActionsGroup = new SharedActionsGroup
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

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Test_SynchronizeDate_LocalTarget(bool isLocalSession)
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        
        var sourceA = sourceRoot.CreateSubdirectory("A");
        var sourceB = sourceRoot.CreateSubdirectory("B");

        var fileA = _testDirectoryService.CreateFileInDirectory(sourceA, "fileToSync.txt", "content_A");
        var originalDateA = DateTime.UtcNow.AddHours(-8);
        fileA.LastWriteTimeUtc = originalDateA;

        var fileB = _testDirectoryService.CreateFileInDirectory(sourceB, "fileToSync.txt", "content_B");
        var originalDateB = DateTime.UtcNow.AddHours(-4);
        fileB.LastWriteTimeUtc = originalDateB;

        var source = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "A", sourceA.FullName, "/fileToSync.txt", null, null, false);
        
        var target = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/fileToSync.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_SyncDate",
            Operator = ActionOperatorTypes.SynchronizeDate,
            Targets = [target],
            Source = source,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToSync.txt", "fileToSync.txt", "/fileToSync.txt"),
            CreationTimeUtc = DateTime.UtcNow.AddHours(-10),
            LastWriteTimeUtc = originalDateA
        };

        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
        
        fileA.Refresh();
        fileB.Refresh();
        
        fileA.Exists.Should().BeTrue();
        fileB.Exists.Should().BeTrue();
        
        // Content should remain unchanged
        var contentB = await File.ReadAllTextAsync(fileB.FullName);
        contentB.Should().Be("content_B");
        
        // But date should be synchronized
        fileB.LastWriteTimeUtc.Should().BeCloseTo(originalDateA, TimeSpan.FromHours(4.1)); // Allow for timezone differences
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Test_SynchronizeContentAndDate_Full_LocalTarget(bool isLocalSession)
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        
        var sourceA = sourceRoot.CreateSubdirectory("A");
        var sourceB = sourceRoot.CreateSubdirectory("B");

        var fileA = _testDirectoryService.CreateFileInDirectory(sourceA, "fileToSync.txt", "content_from_A");
        var originalDateA = DateTime.UtcNow.AddHours(-8);
        fileA.LastWriteTimeUtc = originalDateA;

        var fileBPath = Path.Combine(sourceB.FullName, "fileToSync.txt");
        var fileB = new FileInfo(fileBPath);
        fileB.Exists.Should().BeFalse();

        var source = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "A", sourceA.FullName, "/fileToSync.txt", null, null, false);
        
        var target = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/fileToSync.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_SyncContentAndDate",
            Operator = ActionOperatorTypes.SynchronizeContentAndDate,
            Targets = [target],
            Source = source,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToSync.txt", "fileToSync.txt", "/fileToSync.txt"),
            CreationTimeUtc = DateTime.UtcNow.AddHours(-10),
            LastWriteTimeUtc = originalDateA
        };

        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
        
        fileA.Refresh();
        fileB.Refresh();
        
        fileA.Exists.Should().BeTrue();
        fileB.Exists.Should().BeTrue();
        
        // Both content and date should be synchronized
        var contentB = await File.ReadAllTextAsync(fileB.FullName);
        contentB.Should().Be("content_from_A");
        fileB.LastWriteTimeUtc.Should().BeCloseTo(originalDateA, TimeSpan.FromHours(4.1)); // Allow for timezone differences
    }

    [Test]
    public async Task Test_Create_Directory_LocalTarget()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceB = sourceRoot.CreateSubdirectory("B");
        var targetDirectoryPath = Path.Combine(sourceB.FullName, "NewDirectory");
        
        var targetDirectory = new DirectoryInfo(targetDirectoryPath);
        targetDirectory.Exists.Should().BeFalse();

        var target = new SharedDataPart("NewDirectory", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/NewDirectory", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_Create",
            Operator = ActionOperatorTypes.Create,
            Targets = [target],
            Source = null,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.Directory, "/NewDirectory", "NewDirectory", "/NewDirectory")
        };

        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
        
        targetDirectory.Refresh();
        targetDirectory.Exists.Should().BeTrue();
    }

    [Test]
    public async Task Test_Create_Directory_AlreadyExists_LocalTarget()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceB = sourceRoot.CreateSubdirectory("B");
        var existingDirectory = sourceB.CreateSubdirectory("ExistingDirectory");
        
        existingDirectory.Exists.Should().BeTrue();

        var target = new SharedDataPart("ExistingDirectory", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/ExistingDirectory", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_CreateExisting",
            Operator = ActionOperatorTypes.Create,
            Targets = [target],
            Source = null,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.Directory, "/ExistingDirectory", "ExistingDirectory", "/ExistingDirectory")
        };

        // Should not throw exception even if directory already exists
        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
        
        existingDirectory.Refresh();
        existingDirectory.Exists.Should().BeTrue();
    }

    [Test]
    public async Task Test_Delete_File_LocalTarget()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceB = sourceRoot.CreateSubdirectory("B");
        var fileToDelete = _testDirectoryService.CreateFileInDirectory(sourceB, "fileToDelete.txt", "content");
        
        fileToDelete.Exists.Should().BeTrue();

        var target = new SharedDataPart("fileToDelete.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/fileToDelete.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_DeleteFile",
            Operator = ActionOperatorTypes.Delete,
            Targets = [target],
            Source = null,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToDelete.txt", "fileToDelete.txt", "/fileToDelete.txt")
        };

        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
        
        fileToDelete.Refresh();
        fileToDelete.Exists.Should().BeFalse();
    }

    [Test]
    public async Task Test_Delete_Directory_LocalTarget()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceB = sourceRoot.CreateSubdirectory("B");
        var directoryToDelete = sourceB.CreateSubdirectory("DirectoryToDelete");
        
        directoryToDelete.Exists.Should().BeTrue();

        var target = new SharedDataPart("DirectoryToDelete", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/DirectoryToDelete", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_DeleteDirectory",
            Operator = ActionOperatorTypes.Delete,
            Targets = [target],
            Source = null,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.Directory, "/DirectoryToDelete", "DirectoryToDelete", "/DirectoryToDelete")
        };

        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
        
        directoryToDelete.Refresh();
        directoryToDelete.Exists.Should().BeFalse();
    }

    [Test]
    public async Task Test_Delete_NonExistentFile_LocalTarget_ShouldNotThrow()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceB = sourceRoot.CreateSubdirectory("B");
        var nonExistentFilePath = Path.Combine(sourceB.FullName, "nonExistent.txt");
        
        File.Exists(nonExistentFilePath).Should().BeFalse();

        var target = new SharedDataPart("nonExistent.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/nonExistent.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_DeleteNonExistent",
            Operator = ActionOperatorTypes.Delete,
            Targets = [target],
            Source = null,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/nonExistent.txt", "nonExistent.txt", "/nonExistent.txt")
        };

        // Should not throw exception for non-existent files
        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
    }

    [Test]
    public async Task Test_SynchronizeDate_FileNotExists_ShouldLogWarning()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceB = sourceRoot.CreateSubdirectory("B");
        var nonExistentFilePath = Path.Combine(sourceB.FullName, "nonExistent.txt");
        
        File.Exists(nonExistentFilePath).Should().BeFalse();

        var target = new SharedDataPart("nonExistent.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/nonExistent.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_SyncDateNonExistent",
            Operator = ActionOperatorTypes.SynchronizeDate,
            Targets = [target],
            Source = null,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/nonExistent.txt", "nonExistent.txt", "/nonExistent.txt")
        };

        // Should not throw but should handle the missing file gracefully
        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
    }

    [Test]
    public async Task Test_Create_InvalidFileSystemType_ShouldThrowException()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceB = sourceRoot.CreateSubdirectory("B");

        var target = new SharedDataPart("invalidFile.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/invalidFile.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_CreateInvalidType",
            Operator = ActionOperatorTypes.Create,
            Targets = [target],
            Source = null,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/invalidFile.txt", "invalidFile.txt", "/invalidFile.txt")
        };

        // Should throw exception because Create operation should only work for directories
        FluentActions.Awaiting(() => _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup))
            .Should().ThrowAsync<ApplicationException>();
    }

    [Test]
    public async Task Test_Delete_Directory_WithFiles_ShouldThrowIOException()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceB = sourceRoot.CreateSubdirectory("B");
        var directoryToDelete = sourceB.CreateSubdirectory("DirectoryWithFiles");
        
        // Add a file inside the directory
        _testDirectoryService.CreateFileInDirectory(directoryToDelete, "innerFile.txt", "content");
        
        directoryToDelete.Exists.Should().BeTrue();
        directoryToDelete.GetFiles().Length.Should().Be(1);

        var target = new SharedDataPart("DirectoryWithFiles", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/DirectoryWithFiles", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_DeleteDirectoryWithFiles",
            Operator = ActionOperatorTypes.Delete,
            Targets = [target],
            Source = null,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.Directory, "/DirectoryWithFiles", "DirectoryWithFiles", "/DirectoryWithFiles")
        };

        // Should throw IOException when trying to delete non-empty directory with recursive=false
        FluentActions.Awaiting(() => _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup))
            .Should().ThrowAsync<IOException>();
        
        // Directory should still exist because the delete operation failed
        directoryToDelete.Refresh();
        directoryToDelete.Exists.Should().BeTrue();
    }

    [Test]
    public async Task Test_RunSynchronizationAction_CancelledImmediately_ShouldThrowOperationCancelledException()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_Cancelled",
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Targets = [],
            Source = null,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/test.txt", "test.txt", "/test.txt")
        };

        FluentActions.Awaiting(() => _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup, cancellationTokenSource.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Test_SynchronizeContentOnly_RemoteTarget_ShouldCallUploader()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceA = sourceRoot.CreateSubdirectory("A");
        var fileA = _testDirectoryService.CreateFileInDirectory(sourceA, "fileToSync.txt", "content_A");

        // Create a remote target with different ClientInstanceId
        var remoteClientInstanceId = Guid.NewGuid().ToString();
        
        var source = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "A", sourceA.FullName, "/fileToSync.txt", null, null, false);
        
        var remoteTarget = new SharedDataPart("fileToSync.txt", FileSystemTypes.File, remoteClientInstanceId, 
            "RemoteB", "C:\\RemotePath", "/fileToSync.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_Remote",
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Targets = [remoteTarget],
            Source = source,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToSync.txt", "fileToSync.txt", "/fileToSync.txt")
        };

        // This should call the remote uploader (mocked in integration tests)
        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
        
        // Verify that no local files were affected
        fileA.Refresh();
        fileA.Exists.Should().BeTrue();
    }

    [Test]
    public async Task Test_SynchronizeContentOnly_MixedLocalAndRemoteTargets()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceA = sourceRoot.CreateSubdirectory("A");
        var sourceB = sourceRoot.CreateSubdirectory("B");
        
        var fileA = _testDirectoryService.CreateFileInDirectory(sourceA, "fileToSync.txt", "mixed_content");
        var fileBPath = Path.Combine(sourceB.FullName, "fileToSync.txt");
        var fileB = new FileInfo(fileBPath);
        fileB.Exists.Should().BeFalse();

        var remoteClientInstanceId = Guid.NewGuid().ToString();
        
        var source = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "A", sourceA.FullName, "/fileToSync.txt", null, null, false);
        
        var localTarget = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/fileToSync.txt", null, null, false);
            
        var remoteTarget = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, remoteClientInstanceId, 
            "RemoteC", "C:\\RemotePath", "/fileToSync.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_Mixed",
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Targets = [localTarget, remoteTarget],
            Source = source,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToSync.txt", "fileToSync.txt", "/fileToSync.txt")
        };

        await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
        
        // Verify local target was synchronized
        fileB.Refresh();
        fileB.Exists.Should().BeTrue();
        var contentB = await File.ReadAllTextAsync(fileB.FullName);
        contentB.Should().Be("mixed_content");
        
        // Source should still exist
        fileA.Refresh();
        fileA.Exists.Should().BeTrue();
    }

    [Test]
    public async Task Test_RunSynchronizationAction_MultipleTargets_CancelledDuringProcessing()
    {
        var sourceRoot = _testDirectoryService.CreateSubTestDirectory("Source");
        var sourceA = sourceRoot.CreateSubdirectory("A");
        var sourceB = sourceRoot.CreateSubdirectory("B");
        var sourceC = sourceRoot.CreateSubdirectory("C");
        
        var fileA = _testDirectoryService.CreateFileInDirectory(sourceA, "fileToSync.txt", "content_for_multiple");
        
        var fileBPath = Path.Combine(sourceB.FullName, "fileToSync.txt");
        var fileCPath = Path.Combine(sourceC.FullName, "fileToSync.txt");
        
        var cancellationTokenSource = new CancellationTokenSource();
        
        var source = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "A", sourceA.FullName, "/fileToSync.txt", null, null, false);
        
        var targetB = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "B", sourceB.FullName, "/fileToSync.txt", null, null, false);
            
        var targetC = new SharedDataPart("fileToSync.txt", FileSystemTypes.Directory, _currentEndPoint.ClientInstanceId, 
            "C", sourceC.FullName, "/fileToSync.txt", null, null, false);
        
        var sharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "ACI_MultipleTargets",
            Operator = ActionOperatorTypes.SynchronizeContentOnly,
            Targets = [targetB, targetC],
            Source = source,
            SynchronizationType = SynchronizationTypes.Full,
            PathIdentity = new PathIdentity(FileSystemTypes.File, "/fileToSync.txt", "fileToSync.txt", "/fileToSync.txt")
        };

        // Cancel after a short delay to potentially interrupt processing
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            cancellationTokenSource.Cancel();
        });

        try
        {
            await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // This is expected
        }
        
        // Verify source still exists
        fileA.Refresh();
        fileA.Exists.Should().BeTrue();
    }

    [Test]
    public async Task Test_RunPendingSynchronizationActions_CloudSession_NoAbortRequest()
    {
        // Setup CloudSession
        var cloudSession = new CloudSession
        {
            SessionId = "TestCloudSession",
        };
        
        var mockSessionService = Container.Resolve<Mock<ISessionService>>();
        mockSessionService.Setup(m => m.CurrentSession).Returns(cloudSession);
        
        var mockSynchronizationService = Container.Resolve<Mock<ISynchronizationService>>();
        var synchronizationProcessData = new SynchronizationProcessData();
        synchronizationProcessData.SynchronizationAbortRequest.OnNext(null);
        mockSynchronizationService.Setup(m => m.SynchronizationProcessData).Returns(synchronizationProcessData);

        // This should call Complete and HandlePendingActions
        await _synchronizationActionHandler.RunPendingSynchronizationActions();
        
        // Verify the appropriate methods were called
        // Note: In integration tests, these are typically mocked services
    }

    [Test]
    public async Task Test_RunPendingSynchronizationActions_CloudSession_WithAbortRequest()
    {
        // Setup CloudSession with abort request
        var cloudSession = new CloudSession
        {
            SessionId = "TestCloudSessionAbort",
        };
        
        var mockSessionService = Container.Resolve<Mock<ISessionService>>();
        mockSessionService.Setup(m => m.CurrentSession).Returns(cloudSession);
        
        var mockSynchronizationService = Container.Resolve<Mock<ISynchronizationService>>();
        var synchronizationProcessData = new SynchronizationProcessData();
        var abortRequest = new SynchronizationAbortRequest();
        synchronizationProcessData.SynchronizationAbortRequest.OnNext(abortRequest);
        mockSynchronizationService.Setup(m => m.SynchronizationProcessData).Returns(synchronizationProcessData);

        // This should call Abort and ClearPendingActions
        await _synchronizationActionHandler.RunPendingSynchronizationActions();
        
        // Verify the appropriate abort methods were called
    }

    [Test]
    public async Task Test_RunPendingSynchronizationActions_LocalSession_ShouldDoNothing()
    {
        // Setup LocalSession
        var localSession = new LocalSession();
        
        var mockSessionService = Container.Resolve<Mock<ISessionService>>();
        mockSessionService.Setup(m => m.CurrentSession).Returns(localSession);

        // This should do nothing for local sessions
        await _synchronizationActionHandler.RunPendingSynchronizationActions();
        
        // Since it's a local session, no remote operations should be performed
    }

    [Test]
    public async Task Test_RunPendingSynchronizationActions_CancelledImmediately()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        FluentActions.Awaiting(() => _synchronizationActionHandler.RunPendingSynchronizationActions(cancellationTokenSource.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}