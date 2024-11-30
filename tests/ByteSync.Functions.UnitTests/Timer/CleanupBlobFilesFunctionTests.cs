using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ByteSync.Functions.Timer;
using ByteSync.ServerCommon.Interfaces.Storage;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.Functions.UnitTests.Timer;

[TestFixture]
public class CleanupBlobFilesFunctionTests
{
    private Mock<IBlobContainerProvider> _mockBlobContainerProvider = null!;
    private Mock<ILogger<CleanupBlobFilesFunction>> _mockLogger = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<CleanupBlobFilesFunction>>();
        _mockBlobContainerProvider = new Mock<IBlobContainerProvider>();
    }

    [Test]
    public async Task CleanupFunction_ShouldDeleteBlobsOlderThanRetentionDuration()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string> {
            {"BlobStorage:RetentionDurationInDays", "3"},
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var blobContainerClientMock = new Mock<BlobContainerClient>();
        
        blobContainerClientMock.Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponseMock(true).Object);
        
        _mockBlobContainerProvider.Setup(s => s.GetBlobContainerClient()).Returns(blobContainerClientMock.Object);

        var blobList = new List<BlobItem>()
        {
            BlobsModelFactory.BlobItem("Blob1", false, BuildBlobItemProperties(DateTimeOffset.UtcNow.AddDays(-5))),
            BlobsModelFactory.BlobItem("Blob2", false, BuildBlobItemProperties(DateTimeOffset.UtcNow.AddDays(-4))),
            BlobsModelFactory.BlobItem("Blob3", false, BuildBlobItemProperties(DateTimeOffset.UtcNow.AddDays(1))),
        };
        
        SetBlobContainerClientBlobs(blobContainerClientMock, blobList);

        var cleanupBlobFilesFunction = new CleanupBlobFilesFunction(configuration, _mockBlobContainerProvider.Object , _mockLogger.Object);

        // Act
        int result = await cleanupBlobFilesFunction.RunAsync(new TimerInfo());

        // Assert
        blobContainerClientMock.Verify(c => c.ExistsAsync(It.IsAny<CancellationToken>()), Times.Once);
        blobContainerClientMock.Verify(x => x.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        blobContainerClientMock.Verify(x => x.DeleteBlobAsync("Blob1", DeleteSnapshotsOption.IncludeSnapshots, 
            It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        blobContainerClientMock.Verify(x => x.DeleteBlobAsync("Blob2", DeleteSnapshotsOption.IncludeSnapshots, 
            It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        blobContainerClientMock.Verify(x => x.DeleteBlobAsync("Blob3", DeleteSnapshotsOption.IncludeSnapshots, 
            It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Never);
        blobContainerClientMock.Verify(x => x.DeleteBlobAsync("test2", DeleteSnapshotsOption.IncludeSnapshots,
            It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Never);
        
        result.Should().Be(2);
    }
    
    [Test]
    public async Task CleanupFunction_ShouldNotDeleteIfNoExistingBlob()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string> {
            {"BlobStorage:RetentionDurationInDays", "3"},
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var blobContainerClientMock = new Mock<BlobContainerClient>();
        
        blobContainerClientMock.Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponseMock(true).Object);
        
        _mockBlobContainerProvider.Setup(s => s.GetBlobContainerClient()).Returns(blobContainerClientMock.Object);
        
        SetBlobContainerClientBlobs(blobContainerClientMock, new List<BlobItem>());

        var cleanupBlobFilesFunction = new CleanupBlobFilesFunction(configuration, _mockBlobContainerProvider.Object , _mockLogger.Object);

        // Act
        int result = await cleanupBlobFilesFunction.RunAsync(new TimerInfo());

        // Assert
        blobContainerClientMock.Verify(c => c.ExistsAsync(It.IsAny<CancellationToken>()), Times.Once);
        blobContainerClientMock.Verify(x => x.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        blobContainerClientMock.Verify(x => x.DeleteBlobAsync(It.IsAny<string>(), It.IsAny<DeleteSnapshotsOption>(),
            It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Never);
        
        result.Should().Be(0);
    }
    
    [Test]
    public async Task CleanupFunction_DoesNothingIfContainerDoesNotExist()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string> {
            {"BlobStorage:RetentionDurationInDays", "3"},
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var blobContainerClientMock = new Mock<BlobContainerClient>();

        blobContainerClientMock.Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponseMock(false).Object);
        
        _mockBlobContainerProvider.Setup(s => s.GetBlobContainerClient()).Returns(blobContainerClientMock.Object);
        
        var cleanupBlobFilesFunction = new CleanupBlobFilesFunction(configuration, _mockBlobContainerProvider.Object , _mockLogger.Object);

        // Act
        int result = await cleanupBlobFilesFunction.RunAsync(new TimerInfo());

        // Assert
        blobContainerClientMock.Verify(x => x.GetBlobsAsync(It.IsAny<BlobTraits>(),  It.IsAny<BlobStates>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        blobContainerClientMock.Verify(x => x.DeleteBlobAsync(It.IsAny<string>(), It.IsAny<DeleteSnapshotsOption>(), 
            It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => string.Equals("...Container not found, no element deleted", v.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);

        result.Should().Be(0);
    }
    
    [Test]
    public async Task CleanupFunction_DoesNothingIfRetentionIsTooLow()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string> {
            {"BlobStorage:RetentionDurationInDays", "0"},
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
        
        var cleanupBlobFilesFunction = new CleanupBlobFilesFunction(configuration, _mockBlobContainerProvider.Object , _mockLogger.Object);

        // Act
        int result = await cleanupBlobFilesFunction.RunAsync(new TimerInfo());

        // Assert
        _mockBlobContainerProvider.Verify(x => x.GetBlobContainerClient(), Times.Never);

        _mockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => string.Equals("RetentionDurationInDays is less than 1, no element deleted", v.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);

        result.Should().Be(0);
    }
    
    private static void SetBlobContainerClientBlobs(Mock<BlobContainerClient> blobContainerClientMock, ICollection<BlobItem> blobList)
    {
        var page = Page<BlobItem>.FromValues(blobList.ToArray(), null, Mock.Of<Response>());
        var pageableBlobList = AsyncPageable<BlobItem>.FromPages(new[] { page });

        blobContainerClientMock
            .Setup(m => m.GetBlobsAsync(
                It.IsAny<BlobTraits>(),
                It.IsAny<BlobStates>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(pageableBlobList);
    }
    
    private static Mock<Response<bool>> BuildResponseMock(bool value)
    {
        var responseMock = new Mock<Response<bool>>();
        responseMock.SetupGet(r => r.Value).Returns(value);
        
        return responseMock;
    }

    private static BlobItemProperties BuildBlobItemProperties(DateTimeOffset createdOn)
    {
        return BlobsModelFactory.BlobItemProperties(true, null, null, null, null, null, null, null, null, null, null, 
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 
            null, null, null, null, null, createdOn);
    }
}