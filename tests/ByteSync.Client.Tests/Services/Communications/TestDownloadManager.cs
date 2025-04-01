using ByteSync.Business.Communications;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Transfers;
using ByteSync.TestsCommon;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications;

[TestFixture]
public class TestDownloadManager : AbstractTester
{
    private Mock<IFileDownloaderCache> _mockFileDownloaderCache;
    private Mock<IPostDownloadHandlerProxy> _mockPostDownloadHandlerProxy;
    private Mock<IFileTransferApiClient> _mockFileTransferApiClient;
    private Mock<ILogger<DownloadManager>> _mockLogger;
    private DownloadManager _downloadManager;
    private Mock<ISynchronizationDownloadFinalizer> _mockSynchronizationDownloadFinalizer;

    [SetUp]
    public void SetUp()
    {
        _mockFileDownloaderCache = new Mock<IFileDownloaderCache>(MockBehavior.Strict);
        _mockSynchronizationDownloadFinalizer = new Mock<ISynchronizationDownloadFinalizer>(MockBehavior.Strict);
        _mockPostDownloadHandlerProxy = new Mock<IPostDownloadHandlerProxy>(MockBehavior.Strict);
        _mockFileTransferApiClient = new Mock<IFileTransferApiClient>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<DownloadManager>>();

        _downloadManager = new DownloadManager(
            _mockFileDownloaderCache.Object,
            _mockSynchronizationDownloadFinalizer.Object,
            _mockPostDownloadHandlerProxy.Object,
            _mockFileTransferApiClient.Object,
            _mockLogger.Object
        );
    }
    
    [Test]
    public async Task OnFilePartReadyToDownload_ShouldCall_GetFileDownloader_And_AddAvailablePartAsync()
    {
        // Arrange
        var mockFileDownloader = new Mock<IFileDownloader>(MockBehavior.Loose);
        
        _mockFileDownloaderCache.Setup(m => m.GetFileDownloader(It.IsAny<SharedFileDefinition>()))
            .ReturnsAsync(mockFileDownloader.Object);

        var downloadTarget = new DownloadTarget(new SharedFileDefinition(), null, new HashSet<string>());
        
        mockFileDownloader.Setup(m => m.AddAvailablePartAsync(It.IsAny<int>()));
        mockFileDownloader.Setup(m => m.DownloadTarget).Returns(downloadTarget);
        
        // Act
        await _downloadManager.OnFilePartReadyToDownload(new SharedFileDefinition(), 1);
        
        // Assert
        _mockFileDownloaderCache.Verify(m => m.GetFileDownloader(It.IsAny<SharedFileDefinition>()), Times.Once);
        mockFileDownloader.Verify(m => m.AddAvailablePartAsync(It.IsAny<int>()), Times.Once);
    }
    
    [Test]
    public async Task OnFileReadyToFinalize_ShouldCall_GetFileDownloader_And_SetAllAvailablePartsKnownAsync()
    {
        // Arrange
        var mockFileDownloader = new Mock<IFileDownloader>(MockBehavior.Loose);
        
        _mockFileDownloaderCache.Setup(m => m.GetFileDownloader(It.IsAny<SharedFileDefinition>()))
            .ReturnsAsync(mockFileDownloader.Object);

        var downloadTarget = new DownloadTarget(new SharedFileDefinition(), null, new HashSet<string>());
        
        mockFileDownloader.Setup(m => m.SetAllAvailablePartsKnownAsync(It.IsAny<int>()));
        mockFileDownloader.Setup(m => m.WaitForFileFullyExtracted());
        mockFileDownloader.Setup(m => m.DownloadTarget).Returns(downloadTarget);

        _mockFileTransferApiClient.Setup(m => m.AssertDownloadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        _mockFileDownloaderCache.Setup(m => m.RemoveFileDownloader(It.IsAny<IFileDownloader>()))
            .Returns(Task.CompletedTask);
        _mockPostDownloadHandlerProxy.Setup(m => m.HandleDownloadFinished(It.IsAny<LocalSharedFile>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _downloadManager.OnFileReadyToFinalize(new SharedFileDefinition(), 1);
        
        // Assert
        _mockFileDownloaderCache.Verify(m => m.GetFileDownloader(It.IsAny<SharedFileDefinition>()), Times.Once);
        mockFileDownloader.Verify(m => m.SetAllAvailablePartsKnownAsync(It.IsAny<int>()), Times.Once);
        _mockFileTransferApiClient.Verify(m => m.AssertDownloadIsFinished(It.IsAny<TransferParameters>()), Times.Once);
        _mockFileDownloaderCache.Verify(m => m.RemoveFileDownloader(It.IsAny<IFileDownloader>()), Times.Once);
        _mockPostDownloadHandlerProxy.Verify(m => m.HandleDownloadFinished(It.IsAny<LocalSharedFile>()), Times.Once);
    }
    
    // [Test]
    // public async Task Test_WithDeltaManager_Delta_MultiZipped_1()
    // {
    //     CreateTestDirectory();
    //     var signatureDir = CreateSubTestDirectory("Signatures");
    //     var sourceSignature = new FileInfo(signatureDir.Combine("source.dat"));
    //     var destSignature = new FileInfo(signatureDir.Combine("dest.dat"));
    //     var delta = new FileInfo(signatureDir.Combine("delta.dat"));
    //     
    //     var sourceDir = CreateSubTestDirectory("Source");
    //     var sourceFile = CreateFileInDirectory(sourceDir, "test1.txt", "This is the source contents");
    //     
    //     
    //     var dirA = CreateSubTestDirectory("A");
    //     var fileA = CreateFileInDirectory(dirA, "test1.txt", "contents");
    //     var dirB = CreateSubTestDirectory("B");
    //     var fileB = CreateFileInDirectory(dirB, "test1.txt", "contents");
    //     
    //     // Signature de source
    //     await using (var basisStreamSource = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
    //     {
    //         var signatureBuilderSource = new SignatureBuilder();
    //         await using (var signatureStreamSource = new FileStream(sourceSignature.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
    //         {
    //             await signatureBuilderSource.BuildAsync(basisStreamSource, new SignatureWriter(signatureStreamSource));
    //         }
    //     }
    //
    //     
    //     // Signature de fileA
    //     await using (var basisStream = new FileStream(fileA.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
    //     {
    //         var signatureBuilder = new SignatureBuilder();
    //         await using (var signatureStream = new FileStream(destSignature.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
    //         {
    //             await signatureBuilder.BuildAsync(basisStream, new SignatureWriter(signatureStream));
    //         }
    //     }
    //
    //     
    //     // Calcul du delta Delta
    //     await using var sourceStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
    //     await using var signatureSourceStream = new FileStream(destSignature.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
    //     await using (var deltaStream = new FileStream(delta.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
    //     {
    //         var deltaBuilder = new DeltaBuilder();
    //         await deltaBuilder.BuildDeltaAsync(sourceStream,
    //             new SignatureReader(signatureSourceStream, deltaBuilder.ProgressReport),
    //             new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
    //     }
    //
    //
    //     // On prépare le zip avec le delta à l'intérieur
    //     var dirDownload = CreateSubTestDirectory("Download");
    //     
    //     var downloadedZip = new FileInfo(dirDownload.Combine("download.zip"));
    //     using (ZipArchive zipArchive = ZipFile.Open(downloadedZip.FullName, ZipArchiveMode.Update))
    //     {
    //         zipArchive.CreateEntryFromFile(delta.FullName, "AGID_1");
    //     }
    //     
    //     downloadedZip.Refresh();
    //     ClassicAssert.IsTrue(downloadedZip.Exists);
    //
    //
    //     // MockObjectsGenerator generator = new MockObjectsGenerator(this);
    //     // generator.GenerateCloudSessionManager();
    //     // generator.GenerateGetCurrentEndpoint();
    //
    //     Mock<IFileDownloader> fileDownloader = new Mock<IFileDownloader>();
    //
    //     SharedFileDefinition sharedFileDefinition = new SharedFileDefinition();
    //     sharedFileDefinition.Id = "Test";
    //     sharedFileDefinition.SharedFileType = SharedFileTypes.DeltaSynchronization;
    //     sharedFileDefinition.SessionId = "SessionTest";
    //     // sharedFileDefinition.ActionsGroupIds = new List<string> { "AGID_1"};
    //     
    //     
    //     DownloadTarget downloadTarget = new DownloadTarget(sharedFileDefinition, null, 
    //         new HashSet<string> {downloadedZip.FullName});
    //     downloadTarget.IsMultiFileZip = true;
    //     downloadTarget.FinalDestinationsPerActionsGroupId = new Dictionary<string, HashSet<string>>();
    //     downloadTarget.FinalDestinationsPerActionsGroupId.Add("AGID_1", new HashSet<string> {fileA.FullName, fileB.FullName});
    //
    //     fileDownloader.Setup(m => m.DownloadTarget).Returns(() => downloadTarget);
    //     
    //     // generator.FileDownloadersHandler.Setup(m => m.GetSyncRoot(It.IsAny<SharedFileDefinition>()))
    //     //     .Returns<SharedFileDefinition>(_ => new object());
    //     // generator.FileDownloadersHandler.Setup(m => m.GetFileDownloader(It.IsAny<SharedFileDefinition>()))
    //     //     .Returns<SharedFileDefinition>(_ => fileDownloader.Object);
    //
    //     // DeltaManager deltaManager = new DeltaManager(generator.CloudSessionLocalDataManager.Object, new TemporaryFileManager());
    //     // DownloadManager downloadManager = new DownloadManager(
    //     //     generator.FileDownloadersHandler.Object, generator.ConnectionManager.Object, deltaManager);
    //
    //     await _downloadManager.OnFileReadyToFinalize(sharedFileDefinition, 1);
    //
    //     // generator.HubByteSyncInvokeWrapper.Verify(w => w.AssertDownloadIsFinished(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()), 
    //     //     Times.Exactly(1));
    //     
    //     sourceFile.Refresh();
    //     ClassicAssert.IsTrue(sourceFile.Exists);
    //     fileA.Refresh();
    //     ClassicAssert.IsTrue(fileA.Exists);
    //     fileB.Refresh();
    //     ClassicAssert.IsTrue(fileB.Exists);
    //     downloadedZip.Refresh();
    //     ClassicAssert.IsFalse(downloadedZip.Exists);
    //
    //     string text = await File.ReadAllTextAsync(sourceFile.FullName);
    //     ClassicAssert.AreEqual("This is the source contents", text);
    //     
    //     text = await File.ReadAllTextAsync(fileA.FullName);
    //     ClassicAssert.AreEqual("This is the source contents", text);
    //     
    //     text = await File.ReadAllTextAsync(fileB.FullName);
    //     ClassicAssert.AreEqual("This is the source contents", text);
    // }
}