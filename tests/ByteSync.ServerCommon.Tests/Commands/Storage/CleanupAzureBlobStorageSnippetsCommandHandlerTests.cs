using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Commands.Storage;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Commands.Storage;

[TestFixture]
public class CleanupAzureBlobStorageSnippetsCommandHandlerTests
{
    private IBlobStorageContainerService _blobStorageContainerService = null!;
    private ILogger<CleanupAzureBlobStorageSnippetsCommandHandler> _logger = null!;
    private IOptions<AzureBlobStorageSettings> _options = null!;
    private CleanupAzureBlobStorageSnippetsCommandHandler _handler = null!;
    private AzureBlobStorageSettings _settings = null!;

    [SetUp]
    public void Setup()
    {
        _blobStorageContainerService = A.Fake<IBlobStorageContainerService>();
        _logger = A.Fake<ILogger<CleanupAzureBlobStorageSnippetsCommandHandler>>();
        _settings = new AzureBlobStorageSettings
        {
            RetentionDurationInDays = 3,
            AccountName = "test",
            AccountKey = "test",
            Endpoint = "https://test.blob.core.windows.net/",
            Container = "test"
        };
        _options = A.Fake<IOptions<AzureBlobStorageSettings>>();
        A.CallTo(() => _options.Value).Returns(_settings);

        _handler = new CleanupAzureBlobStorageSnippetsCommandHandler(_blobStorageContainerService, _options, _logger);
    }

    [Test]
    public async Task Handle_DeletesBlobsOlderThanRetention()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var blobs = new List<BlobItem>
        {
            BlobsModelFactory.BlobItem("old1", false, BuildBlobItemProperties(now.AddDays(-5))),
            BlobsModelFactory.BlobItem("old2", false, BuildBlobItemProperties(now.AddDays(-4))),
            BlobsModelFactory.BlobItem("recent", false, BuildBlobItemProperties(now.AddDays(-1))),
        };

        var container = A.Fake<BlobContainerClient>();
        A.CallTo(() => container.ExistsAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(Response.FromValue(true, default)));
        A.CallTo(() => container.GetBlobsAsync(A<BlobTraits>._, A<BlobStates>._, null, A<CancellationToken>._))
            .Returns(new TestAsyncPageable<BlobItem>(blobs));
        A.CallTo(() => _blobStorageContainerService.BuildBlobContainerClient())
            .Returns(Task.FromResult(container));

        // Act
        var result = await _handler.Handle(new CleanupAzureBlobStorageSnippetsRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(2);
        A.CallTo(() => container.DeleteBlobAsync("old1", DeleteSnapshotsOption.IncludeSnapshots, null, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => container.DeleteBlobAsync("old2", DeleteSnapshotsOption.IncludeSnapshots, null, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => container.DeleteBlobAsync("recent", DeleteSnapshotsOption.IncludeSnapshots, null, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_DoesNothingIfRetentionIsTooLow()
    {
        // Arrange
        _settings.RetentionDurationInDays = 0;

        // Act
        var result = await _handler.Handle(new CleanupAzureBlobStorageSnippetsRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(0);
        A.CallTo(() => _blobStorageContainerService.BuildBlobContainerClient()).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_DoesNothingIfContainerDoesNotExist()
    {
        // Arrange
        var container = A.Fake<BlobContainerClient>();
        A.CallTo(() => container.ExistsAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(Response.FromValue(false, default)));
        A.CallTo(() => _blobStorageContainerService.BuildBlobContainerClient())
            .Returns(Task.FromResult(container));

        // Act
        var result = await _handler.Handle(new CleanupAzureBlobStorageSnippetsRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Test]
    public async Task Handle_ReturnsZeroIfNoBlobsToDelete()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var blobs = new List<BlobItem>
        {
            BlobsModelFactory.BlobItem("recent1", false, BuildBlobItemProperties(now.AddDays(-1))),
            BlobsModelFactory.BlobItem("recent2", false, BuildBlobItemProperties(now)),
        };

        var container = A.Fake<BlobContainerClient>();
        A.CallTo(() => container.ExistsAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(Response.FromValue(true, default)));
        A.CallTo(() => container.GetBlobsAsync(A<BlobTraits>._, A<BlobStates>._, null, A<CancellationToken>._))
            .Returns(new TestAsyncPageable<BlobItem>(blobs));
        A.CallTo(() => _blobStorageContainerService.BuildBlobContainerClient())
            .Returns(Task.FromResult(container));

        // Act
        var result = await _handler.Handle(new CleanupAzureBlobStorageSnippetsRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(0);
        A.CallTo(() => container.DeleteBlobAsync(A<string>._, DeleteSnapshotsOption.IncludeSnapshots, null, A<CancellationToken>._)).MustNotHaveHappened();
    }

    // Helper to create BlobItemProperties with only CreatedOn set
    private static BlobItemProperties BuildBlobItemProperties(DateTimeOffset createdOn)
    {
        // Adapter la signature à la version installée d'Azure.Storage.Blobs
        return BlobsModelFactory.BlobItemProperties(
            lastModified: null,
            contentLength: null,
            contentType: null,
            contentEncoding: null,
            contentLanguage: null,
            contentHash: null,
            contentDisposition: null,
            cacheControl: null,
            blobSequenceNumber: null,
            blobType: null,
            leaseStatus: null,
            leaseState: null,
            leaseDuration: null,
            copyId: null,
            copyStatus: null,
            copySource: null,
            copyProgress: null,
            serverEncrypted: null,
            incrementalCopy: null,
            destinationSnapshot: null,
            remainingRetentionDays: null,
            accessTier: null,
            accessTierInferred: false,
            archiveStatus: null,
            customerProvidedKeySha256: null,
            encryptionScope: null,
            tagCount: null,
            expiresOn: null,
            createdOn: createdOn
        );
    }

    // Helper for AsyncPageable<BlobItem>
    private class TestAsyncPageable<T> : AsyncPageable<T> where T : notnull
    {
        private readonly IEnumerable<T> _items;
        
        public TestAsyncPageable(IEnumerable<T> items) => _items = items;
        
        public override async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (var item in _items)
                yield return item;
            await Task.CompletedTask;
        }
        
        public override IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            throw new ApplicationException(); // Not needed for these tests
        }
    }
} 