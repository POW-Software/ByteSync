using Autofac;
using ByteSync.ServerCommon.Services.Storage;
using ByteSync.ServerCommon.Business.Settings;
using Microsoft.Extensions.Options;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.Integration.Services;

[TestFixture]
public class AzureBlobStorageService_Tests
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

    [Test]
    public void TestWithSettings()
    {
        var azureBlobStorageService = _scope.Resolve<AzureBlobStorageService>();
        azureBlobStorageService.Should().NotBeNull();
    }

    [Test]
    public void StorageSharedKeyCredential_ShouldMatchConfiguredAccountName()
    {
        var options = _scope.Resolve<IOptions<AzureBlobStorageSettings>>();
        var factory = _scope.Resolve<IAzureBlobContainerClientFactory>();
        options.Value.Should().NotBeNull();
        factory.GetCredential().AccountName.Should().Be(options.Value.AccountName);
    }

    [Test]
    public async Task GetUploadAndDownloadUrls_ShouldContainSas_WithExpectedPermissions()
    {
        var service = _scope.Resolve<AzureBlobStorageService>();
        var shared = new SharedFileDefinition
        {
            StorageObjectId = Guid.NewGuid().ToString("N"),
            SessionId = "sess123",
            ClientInstanceId = "cliABC",
            SharedFileType = SharedFileTypes.FullSynchronization,
            AdditionalName = "ignored"
        };
        int partNumber = 2;

        var uploadUrl = await service.GetUploadFileUrl(shared, partNumber);
        var downloadUrl = await service.GetDownloadFileUrl(shared, partNumber);

        uploadUrl.Should().NotBeNullOrEmpty();
        downloadUrl.Should().NotBeNullOrEmpty();

        var uploadUri = new Uri(uploadUrl);
        var downloadUri = new Uri(downloadUrl);

        var uploadQuery = System.Web.HttpUtility.ParseQueryString(uploadUri.Query);
        var downloadQuery = System.Web.HttpUtility.ParseQueryString(downloadUri.Query);
        uploadQuery["se"].Should().NotBeNull(); // expiry
        uploadQuery["sp"].Should().NotBeNull(); // permissions
        downloadQuery["se"].Should().NotBeNull();
        downloadQuery["sp"].Should().NotBeNull();

        uploadQuery["sp"].Should().Be("w");
        downloadQuery["sp"].Should().Be("r");
        uploadUri.AbsolutePath.Should().EndWith($"/{shared.StorageObjectId}/part-{partNumber}");
        downloadUri.AbsolutePath.Should().Be(uploadUri.AbsolutePath);
        DateTimeOffset.Parse(uploadQuery["se"]!).Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(15));
    }

    [Test]
    public async Task Urls_ShouldDiffer_AndPartNumberImpactsKey()
    {
        var service = _scope.Resolve<AzureBlobStorageService>();
        var shared = new SharedFileDefinition
        {
            StorageObjectId = Guid.NewGuid().ToString("N"),
            SessionId = "sessA",
            ClientInstanceId = "cliA",
            SharedFileType = SharedFileTypes.FullSynchronization
        };

        var url1 = await service.GetUploadFileUrl(shared, 1);
        var url2 = await service.GetUploadFileUrl(shared, 2);
        url1.Should().NotBe(url2);
        new Uri(url1).AbsolutePath.Should().EndWith($"/{shared.StorageObjectId}/part-1");
        new Uri(url2).AbsolutePath.Should().EndWith($"/{shared.StorageObjectId}/part-2");
    }

    [Test]
    public async Task InventoryType_ShouldUseServerObjectIdWithoutEmbeddingClientMetadata()
    {
        var service = _scope.Resolve<AzureBlobStorageService>();
        var shared = new SharedFileDefinition
        {
            StorageObjectId = Guid.NewGuid().ToString("N"),
            SessionId = "sessInvA",
            ClientInstanceId = "cliInvA",
            SharedFileType = SharedFileTypes.BaseInventory,
            AdditionalName = "profileABC"
        };

        var url = await service.GetDownloadFileUrl(shared, 1);
        var uri = new Uri(url);
        uri.AbsolutePath.Should().EndWith($"/{shared.StorageObjectId}/part-1");
        uri.AbsolutePath.Should().NotContain(shared.SessionId).And.NotContain(shared.ClientInstanceId).And.NotContain(shared.AdditionalName);
        shared.AdditionalName = "different-profile-name";
        var renamed = new Uri(await service.GetDownloadFileUrl(shared, 1));
        renamed.AbsolutePath.Should().Be(uri.AbsolutePath);
    }
}