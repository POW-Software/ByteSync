using ByteSync.Business.Communications;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Profiles;
using ByteSync.Services.Communications.Transfers.Downloading;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Downloading;

[TestFixture]
public class PostDownloadHandlerProxyTests
{
    private Mock<ISessionProfileManager> _sessionProfileManager = null!;
    private Mock<ISynchronizationDataReceiver> _synchronizationDataReceiver = null!;
    private Mock<IInventoryService> _inventoryService = null!;
    private PostDownloadHandlerProxy _postDownloadHandlerProxy = null!;
    
    [SetUp]
    public void Setup()
    {
        _sessionProfileManager = new Mock<ISessionProfileManager>();
        _synchronizationDataReceiver = new Mock<ISynchronizationDataReceiver>();
        _inventoryService = new Mock<IInventoryService>();
        
        _postDownloadHandlerProxy = new PostDownloadHandlerProxy(
            _sessionProfileManager.Object,
            _inventoryService.Object,
            _synchronizationDataReceiver.Object);
    }
    
    [Test]
    public void Constructor_ShouldInitializeAllDependencies()
    {
        // Arrange & Act
        var proxy = new PostDownloadHandlerProxy(
            _sessionProfileManager.Object,
            _inventoryService.Object,
            _synchronizationDataReceiver.Object);
        
        // Assert
        proxy.Should().NotBeNull();
    }
    
    [Test]
    public async Task HandleDownloadFinished_WithNullLocalSharedFile_ShouldNotCallAnyService()
    {
        // Arrange
        LocalSharedFile? localSharedFile = null;
        
        // Act
        await _postDownloadHandlerProxy.HandleDownloadFinished(localSharedFile);
        
        // Assert
        _sessionProfileManager.Verify(x => x.OnFileIsFullyDownloaded(It.IsAny<LocalSharedFile>()), Times.Never);
        _synchronizationDataReceiver.Verify(x => x.OnSynchronizationDataFileDownloaded(It.IsAny<LocalSharedFile>()), Times.Never);
        _inventoryService.Verify(x => x.OnFileIsFullyDownloaded(It.IsAny<LocalSharedFile>()), Times.Never);
    }
    
    [Test]
    public async Task HandleDownloadFinished_WithProfileDetailsFile_ShouldCallSessionProfileManager()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            SharedFileType = SharedFileTypes.ProfileDetails
        };
        var localSharedFile = new LocalSharedFile(sharedFileDefinition, "test-path");
        
        // Act
        await _postDownloadHandlerProxy.HandleDownloadFinished(localSharedFile);
        
        // Assert
        _sessionProfileManager.Verify(x => x.OnFileIsFullyDownloaded(localSharedFile), Times.Once);
        _synchronizationDataReceiver.Verify(x => x.OnSynchronizationDataFileDownloaded(It.IsAny<LocalSharedFile>()), Times.Never);
        _inventoryService.Verify(x => x.OnFileIsFullyDownloaded(It.IsAny<LocalSharedFile>()), Times.Never);
    }
    
    [Test]
    public async Task HandleDownloadFinished_WithSynchronizationStartDataFile_ShouldCallSynchronizationDataReceiver()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            SharedFileType = SharedFileTypes.SynchronizationStartData
        };
        var localSharedFile = new LocalSharedFile(sharedFileDefinition, "test-path");
        
        // Act
        await _postDownloadHandlerProxy.HandleDownloadFinished(localSharedFile);
        
        // Assert
        _sessionProfileManager.Verify(x => x.OnFileIsFullyDownloaded(It.IsAny<LocalSharedFile>()), Times.Never);
        _synchronizationDataReceiver.Verify(x => x.OnSynchronizationDataFileDownloaded(localSharedFile), Times.Once);
        _inventoryService.Verify(x => x.OnFileIsFullyDownloaded(It.IsAny<LocalSharedFile>()), Times.Never);
    }
    
    [Test]
    public async Task HandleDownloadFinished_WithInventoryFile_ShouldCallInventoryService()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            SharedFileType = SharedFileTypes.BaseInventory
        };
        var localSharedFile = new LocalSharedFile(sharedFileDefinition, "test-path");
        
        // Act
        await _postDownloadHandlerProxy.HandleDownloadFinished(localSharedFile);
        
        // Assert
        _sessionProfileManager.Verify(x => x.OnFileIsFullyDownloaded(It.IsAny<LocalSharedFile>()), Times.Never);
        _synchronizationDataReceiver.Verify(x => x.OnSynchronizationDataFileDownloaded(It.IsAny<LocalSharedFile>()), Times.Never);
        _inventoryService.Verify(x => x.OnFileIsFullyDownloaded(localSharedFile), Times.Once);
    }
    
    [Test]
    public async Task HandleDownloadFinished_WhenSessionProfileManagerThrows_ShouldPropagateException()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            SharedFileType = SharedFileTypes.ProfileDetails
        };
        var localSharedFile = new LocalSharedFile(sharedFileDefinition, "test-path");
        var expectedException = new InvalidOperationException("Test exception");
        
        _sessionProfileManager.Setup(x => x.OnFileIsFullyDownloaded(localSharedFile))
            .ThrowsAsync(expectedException);
        
        // Act & Assert
        var exception = await FluentActions.Invoking(async () =>
                await _postDownloadHandlerProxy.HandleDownloadFinished(localSharedFile))
            .Should().ThrowAsync<InvalidOperationException>();
        
        exception.Which.Should().BeSameAs(expectedException);
    }
    
    [Test]
    public async Task HandleDownloadFinished_WhenSynchronizationDataReceiverThrows_ShouldPropagateException()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            SharedFileType = SharedFileTypes.SynchronizationStartData
        };
        var localSharedFile = new LocalSharedFile(sharedFileDefinition, "test-path");
        var expectedException = new InvalidOperationException("Test exception");
        
        _synchronizationDataReceiver.Setup(x => x.OnSynchronizationDataFileDownloaded(localSharedFile))
            .ThrowsAsync(expectedException);
        
        // Act & Assert
        var exception = await FluentActions.Invoking(async () =>
                await _postDownloadHandlerProxy.HandleDownloadFinished(localSharedFile))
            .Should().ThrowAsync<InvalidOperationException>();
        
        exception.Which.Should().BeSameAs(expectedException);
    }
    
    [Test]
    public async Task HandleDownloadFinished_WhenInventoryServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            SharedFileType = SharedFileTypes.BaseInventory
        };
        var localSharedFile = new LocalSharedFile(sharedFileDefinition, "test-path");
        var expectedException = new InvalidOperationException("Test exception");
        
        _inventoryService.Setup(x => x.OnFileIsFullyDownloaded(localSharedFile))
            .ThrowsAsync(expectedException);
        
        // Act & Assert
        var exception = await FluentActions.Invoking(async () =>
                await _postDownloadHandlerProxy.HandleDownloadFinished(localSharedFile))
            .Should().ThrowAsync<InvalidOperationException>();
        
        exception.Which.Should().BeSameAs(expectedException);
    }
}