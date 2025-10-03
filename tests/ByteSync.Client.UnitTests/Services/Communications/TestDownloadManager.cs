using System.Reflection;
using ByteSync.Business.Communications;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Transfers.Downloading;
using ByteSync.TestsCommon;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications;

[TestFixture]
public class TestDownloadManager : AbstractTester
{
    private Mock<IFileDownloaderCache> _mockFileDownloaderCache = null!;
    private Mock<IPostDownloadHandlerProxy> _mockPostDownloadHandlerProxy = null!;
    private Mock<IFileTransferApiClient> _mockFileTransferApiClient = null!;
    private Mock<ILogger<DownloadManager>> _mockLogger = null!;
    private DownloadManager _downloadManager = null!;
    private Mock<ISynchronizationDownloadFinalizer> _mockSynchronizationDownloadFinalizer = null!;
    
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
        var mockCoordinator = new Mock<IDownloadPartsCoordinator>(MockBehavior.Strict);
        var sharedFileDefinition = new SharedFileDefinition();
        var downloadTarget = new DownloadTarget(sharedFileDefinition, null, new HashSet<string>());
        
        _mockFileDownloaderCache.Setup(m => m.GetFileDownloader(It.IsAny<SharedFileDefinition>()))
            .ReturnsAsync(mockFileDownloader.Object);
        mockFileDownloader.Setup(m => m.DownloadTarget).Returns(downloadTarget);
        var partsCoordinatorCacheField =
            typeof(DownloadManager).GetField("_partsCoordinatorCache", BindingFlags.NonPublic | BindingFlags.Instance);
        var cache = (IDictionary<string, IDownloadPartsCoordinator>)partsCoordinatorCacheField!.GetValue(_downloadManager)!;
        cache[sharedFileDefinition.Id] = mockCoordinator.Object;
        mockCoordinator.Setup(m => m.AddAvailablePartAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        
        // Act
        await _downloadManager.OnFilePartReadyToDownload(sharedFileDefinition, 1);
        
        // Assert
        _mockFileDownloaderCache.Verify(m => m.GetFileDownloader(It.IsAny<SharedFileDefinition>()), Times.Once);
        mockCoordinator.Verify(m => m.AddAvailablePartAsync(It.IsAny<int>()), Times.Once);
    }
    
    [Test]
    public async Task OnFileReadyToFinalize_ShouldCall_GetFileDownloader_And_SetAllPartsKnownAsync()
    {
        // Arrange
        var mockFileDownloader = new Mock<IFileDownloader>(MockBehavior.Loose);
        var mockCoordinator = new Mock<IDownloadPartsCoordinator>(MockBehavior.Strict);
        var sharedFileDefinition = new SharedFileDefinition();
        var downloadTarget = new DownloadTarget(sharedFileDefinition, null, new HashSet<string>());
        
        _mockFileDownloaderCache.Setup(m => m.GetFileDownloader(It.IsAny<SharedFileDefinition>()))
            .ReturnsAsync(mockFileDownloader.Object);
        mockFileDownloader.Setup(m => m.DownloadTarget).Returns(downloadTarget);
        mockFileDownloader.Setup(m => m.StartDownload()).Returns(Task.CompletedTask);
        mockFileDownloader.Setup(m => m.WaitForFileFullyExtracted()).Returns(Task.CompletedTask);
        var partsCoordinatorCacheField =
            typeof(DownloadManager).GetField("_partsCoordinatorCache", BindingFlags.NonPublic | BindingFlags.Instance);
        var cache = (IDictionary<string, IDownloadPartsCoordinator>)partsCoordinatorCacheField!.GetValue(_downloadManager)!;
        cache[sharedFileDefinition.Id] = mockCoordinator.Object;
        mockCoordinator.Setup(m => m.SetAllPartsKnownAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _mockFileTransferApiClient.Setup(m => m.AssertDownloadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        _mockFileDownloaderCache.Setup(m => m.RemoveFileDownloader(It.IsAny<IFileDownloader>()))
            .Returns(Task.CompletedTask);
        _mockPostDownloadHandlerProxy.Setup(m => m.HandleDownloadFinished(It.IsAny<LocalSharedFile>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _downloadManager.OnFileReadyToFinalize(sharedFileDefinition, 1);
        
        // Assert
        _mockFileDownloaderCache.Verify(m => m.GetFileDownloader(It.IsAny<SharedFileDefinition>()), Times.Once);
        mockCoordinator.Verify(m => m.SetAllPartsKnownAsync(It.IsAny<int>()), Times.Once);
        _mockFileTransferApiClient.Verify(m => m.AssertDownloadIsFinished(It.IsAny<TransferParameters>()), Times.Once);
        _mockFileDownloaderCache.Verify(m => m.RemoveFileDownloader(It.IsAny<IFileDownloader>()), Times.Once);
        _mockPostDownloadHandlerProxy.Verify(m => m.HandleDownloadFinished(It.IsAny<LocalSharedFile>()), Times.Once);
    }
}