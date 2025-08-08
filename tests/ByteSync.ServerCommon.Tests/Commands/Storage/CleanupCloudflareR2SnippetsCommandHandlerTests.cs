using Amazon.S3;
using Amazon.S3.Model;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Commands.Storage;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Commands.Storage;

[TestFixture]
public class CleanupCloudflareR2SnippetsCommandHandlerTests
{
    private ICloudflareR2Service _cloudflareR2Service = null!;
    private ILogger<CleanupCloudflareR2SnippetsCommandHandler> _logger = null!;
    private IOptions<CloudflareR2Settings> _options = null!;
    private CleanupCloudflareR2SnippetsCommandHandler _handler = null!;
    private CloudflareR2Settings _settings = null!;

    [SetUp]
    public void Setup()
    {
        _cloudflareR2Service = A.Fake<ICloudflareR2Service>();
        _logger = A.Fake<ILogger<CleanupCloudflareR2SnippetsCommandHandler>>();
        _settings = new CloudflareR2Settings
        {
            RetentionDurationInDays = 3,
            AccessKeyId = "test",
            SecretAccessKey = "test",
            Endpoint = "https://test.r2.cloudflarestorage.com",
            BucketName = "test-bucket"
        };
        _options = A.Fake<IOptions<CloudflareR2Settings>>();
        A.CallTo(() => _options.Value).Returns(_settings);

        _handler = new CleanupCloudflareR2SnippetsCommandHandler(_cloudflareR2Service, _options, _logger);
    }

    [Test]
    public async Task Handle_DeletesObjectsOlderThanRetention()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var s3Objects = new List<S3Object>
        {
            new S3Object { Key = "old1", LastModified = now.AddDays(-5) },
            new S3Object { Key = "old2", LastModified = now.AddDays(-4) },
            new S3Object { Key = "recent", LastModified = now.AddDays(-1) },
        };

        var listObjectsResponse = new ListObjectsV2Response
        {
            S3Objects = s3Objects
        };

        A.CallTo(() => _cloudflareR2Service.ListObjectsAsync(A<ListObjectsV2Request>._, A<CancellationToken>._))
            .Returns(listObjectsResponse);

        // Act
        var result = await _handler.Handle(new CleanupCloudflareR2SnippetsRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(2);
        A.CallTo(() => _cloudflareR2Service.DeleteObjectAsync(A<DeleteObjectRequest>.That.Matches(r => r.Key == "old1"), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _cloudflareR2Service.DeleteObjectAsync(A<DeleteObjectRequest>.That.Matches(r => r.Key == "old2"), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _cloudflareR2Service.DeleteObjectAsync(A<DeleteObjectRequest>.That.Matches(r => r.Key == "recent"), A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_DoesNothingIfRetentionIsTooLow()
    {
        // Arrange
        _settings.RetentionDurationInDays = 0;

        // Act
        var result = await _handler.Handle(new CleanupCloudflareR2SnippetsRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(0);
        A.CallTo(() => _cloudflareR2Service.ListObjectsAsync(A<ListObjectsV2Request>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_ReturnsZeroIfNoObjectsToDelete()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var s3Objects = new List<S3Object>
        {
            new S3Object { Key = "recent1", LastModified = now.AddDays(-1) },
            new S3Object { Key = "recent2", LastModified = now },
        };

        var listObjectsResponse = new ListObjectsV2Response
        {
            S3Objects = s3Objects
        };

        A.CallTo(() => _cloudflareR2Service.ListObjectsAsync(A<ListObjectsV2Request>._, A<CancellationToken>._))
            .Returns(listObjectsResponse);

        // Act
        var result = await _handler.Handle(new CleanupCloudflareR2SnippetsRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(0);
        A.CallTo(() => _cloudflareR2Service.DeleteObjectAsync(A<DeleteObjectRequest>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_ReturnsZeroOnS3Exception()
    {
        // Arrange
        A.CallTo(() => _cloudflareR2Service.ListObjectsAsync(A<ListObjectsV2Request>._, A<CancellationToken>._))
            .Throws(new AmazonS3Exception("Test exception"));

        // Act
        var result = await _handler.Handle(new CleanupCloudflareR2SnippetsRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }
} 