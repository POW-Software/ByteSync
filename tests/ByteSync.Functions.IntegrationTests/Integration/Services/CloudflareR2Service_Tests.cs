using Autofac;
using ByteSync.ServerCommon.Services.Storage;
using ByteSync.ServerCommon.Business.Settings;
using Microsoft.Extensions.Options;
using ByteSync.Common.Business.SharedFiles;
using Microsoft.Extensions.Configuration;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.Integration.Services;

[TestFixture]
public class CloudflareR2Service_Tests
{
    private ILifetimeScope _scope;

    [SetUp]
    public void Setup()
    {
        _scope = GlobalTestSetup.Container.BeginLifetimeScope();
    }

    [TearDown]
    public void Teardown()
    {
        _scope.Dispose();
    }
    
    /*[Test]
    public void Settings_ShouldBeLoadedFromConfiguration()
    {
        var section = GlobalTestSetup.Configuration.GetSection("CloudflareR2");
        section.Exists().Should().BeTrue();
        section["Endpoint"].Should().NotBeNullOrEmpty();
        section["BucketName"].Should().NotBeNullOrEmpty();
        section["RetentionDurationInDays"].Should().NotBeNullOrEmpty();
    }

    [Test]
    public void BuildS3Client_WithSettings_ShouldReturnClient()
    {
        var cloudflareR2Service = _scope.Resolve<CloudflareR2Service>();
        var client = cloudflareR2Service.BuildS3Client();
        client.Should().NotBeNull();
    }

    [Test]
    public void BuildS3Client_ShouldBeSingletonWithinScope()
    {
        var service = _scope.Resolve<CloudflareR2Service>();
        var client1 = service.BuildS3Client();
        var client2 = service.BuildS3Client();
        client1.Should().BeSameAs(client2);
    }

    [Test]
    public async Task GetUploadAndDownloadUrls_ShouldContainBucketAndKey_WithExpectedExpiry()
    {
        // Arrange
        var options = _scope.Resolve<IOptions<CloudflareR2Settings>>();
        var service = _scope.Resolve<CloudflareR2Service>();
        var shared = new SharedFileDefinition
        {
            SessionId = "sess123",
            ClientInstanceId = "cliABC",
            SharedFileType = SharedFileTypes.FullSynchronization,
            AdditionalName = "ignored"
        };
        int partNumber = 1;

        // Act
        var uploadUrl = await service.GetUploadFileUrl(shared, partNumber);
        var downloadUrl = await service.GetDownloadFileUrl(shared, partNumber);

        // Assert
        uploadUrl.Should().NotBeNullOrEmpty();
        downloadUrl.Should().NotBeNullOrEmpty();

        var uploadUri = new Uri(uploadUrl);
        var downloadUri = new Uri(downloadUrl);

        var expectedKey = shared.SessionId + "_" + shared.ClientInstanceId + "_" + shared.GetFileName(partNumber);
        var expectedUploadPath = "/" + options.Value.BucketName + "/" + expectedKey;
        uploadUri.AbsolutePath.Should().Be(expectedUploadPath);
        downloadUri.AbsolutePath.Should().Be(expectedUploadPath);

        var uploadQuery = System.Web.HttpUtility.ParseQueryString(uploadUri.Query);
        var downloadQuery = System.Web.HttpUtility.ParseQueryString(downloadUri.Query);

        uploadQuery["X-Amz-Algorithm"].Should().NotBeNull();
        uploadQuery["X-Amz-Credential"].Should().NotBeNull();
        uploadQuery["X-Amz-Expires"].Should().NotBeNull();
        downloadQuery["X-Amz-Algorithm"].Should().NotBeNull();
        downloadQuery["X-Amz-Credential"].Should().NotBeNull();
        downloadQuery["X-Amz-Expires"].Should().NotBeNull();

        // Upload is 60 minutes ~ 3600 seconds; download is 20 minutes ~ 1200 seconds
        var uploadExpires = int.Parse(uploadQuery["X-Amz-Expires"]!);
        var downloadExpires = int.Parse(downloadQuery["X-Amz-Expires"]!);
        uploadExpires.Should().BeInRange(3500, 3700);
        downloadExpires.Should().BeInRange(1100, 1300);
    }

    [Test]
    public async Task Urls_ShouldDiffer_AndPartNumberImpactsKey()
    {
        var service = _scope.Resolve<CloudflareR2Service>();
        var shared = new SharedFileDefinition
        {
            SessionId = "sessZ",
            ClientInstanceId = "cliZ",
            SharedFileType = SharedFileTypes.FullSynchronization
        };

        var url1 = await service.GetUploadFileUrl(shared, 1);
        var url2 = await service.GetUploadFileUrl(shared, 2);
        url1.Should().NotBe(url2);

        var uri1 = new Uri(url1);
        var uri2 = new Uri(url2);
        uri1.AbsolutePath.Should().NotBe(uri2.AbsolutePath);
    }

    [Test]
    public async Task InventoryType_ShouldUseAdditionalNameInKey()
    {
        var options = _scope.Resolve<IOptions<CloudflareR2Settings>>();
        var service = _scope.Resolve<CloudflareR2Service>();
        var shared = new SharedFileDefinition
        {
            SessionId = "sessInv",
            ClientInstanceId = "cliInv",
            SharedFileType = SharedFileTypes.BaseInventory,
            AdditionalName = "profile123"
        };

        var url = await service.GetDownloadFileUrl(shared, 1);
        var uri = new Uri(url);
        var expectedBlobName = shared.SessionId + "_" + shared.ClientInstanceId + "_" + shared.GetFileName(1);
        var expectedPath = "/" + options.Value.BucketName + "/" + expectedBlobName;
        uri.AbsolutePath.Should().Be(expectedPath);
        expectedBlobName.Should().Contain("base_inventory_profile123");
    }*/
}