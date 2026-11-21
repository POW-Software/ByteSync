using Azure.Storage;
using Azure.Storage.Blobs;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using ByteSync.ServerCommon.Services.Storage;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ByteSync.ServerCommon.Tests.Security;

public class StorageUrlScopeSecurityTests
{
    [TestCase(true, "w", SharedFileTypes.FullSynchronization)]
    [TestCase(false, "r", SharedFileTypes.FullSynchronization)]
    [TestCase(true, "w", SharedFileTypes.BaseInventory)]
    [TestCase(false, "r", SharedFileTypes.BaseInventory)]
    [TestCase(true, "w", SharedFileTypes.FullInventory)]
    [TestCase(false, "r", SharedFileTypes.ProfileDetails)]
    public async Task AzureSas_IsLimitedToTheRegisteredObjectPartAndExpectedOperation(bool upload, string permission, SharedFileTypes type)
    {
        var factory = A.Fake<IAzureBlobContainerClientFactory>();
        var credential = new StorageSharedKeyCredential("testaccount", Convert.ToBase64String(new byte[32]));
        A.CallTo(() => factory.GetCredential()).Returns(credential);
        A.CallTo(() => factory.GetOrCreateContainer(A<CancellationToken>._)).Returns(new BlobContainerClient(new Uri("https://testaccount.blob.core.windows.net/files"), credential));
        var service = new AzureBlobStorageService(factory, NullLogger<AzureBlobStorageService>.Instance);
        var objectId = Guid.NewGuid().ToString("N");
        var definition = new SharedFileDefinition { StorageObjectId = objectId, Id = "untrusted", SessionId = "untrusted",
            SharedFileType = type, AdditionalName = "../untrusted" };

        var url = upload ? await service.GetUploadFileUrl(definition, 7) : await service.GetDownloadFileUrl(definition, 7);
        var uri = new Uri(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        uri.AbsolutePath.Should().Be($"/files/{objectId}/part-7");
        query["sp"].Should().Be(permission);
        query["sr"].Should().Be("b");
        DateTimeOffset.Parse(query["se"]!).Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
        definition.AdditionalName = "another-inventory-name";
        var renamed = upload ? await service.GetUploadFileUrl(definition, 7) : await service.GetDownloadFileUrl(definition, 7);
        new Uri(renamed).AbsolutePath.Should().Be(uri.AbsolutePath);
    }
    [TestCase(true)]
    [TestCase(false)]
    public async Task R2SigningRequest_IsLimitedToTheServerObjectPartAndOperation(bool upload)
    {
        var client = A.Fake<Amazon.S3.IAmazonS3>();
        Amazon.S3.Model.GetPreSignedUrlRequest? signed = null;
        A.CallTo(() => client.GetPreSignedURLAsync(A<Amazon.S3.Model.GetPreSignedUrlRequest>._))
            .Invokes((Amazon.S3.Model.GetPreSignedUrlRequest request) => signed = request).Returns("offline-signed-url");
        var factory = A.Fake<ICloudflareR2ClientFactory>();
        A.CallTo(() => factory.GetClient()).Returns(client);
        var service = new CloudflareR2Service(Microsoft.Extensions.Options.Options.Create(new ByteSync.ServerCommon.Business.Settings.CloudflareR2Settings { BucketName = "local-test-bucket" }),
            factory, NullLogger<CloudflareR2Service>.Instance);
        var objectId = Guid.NewGuid().ToString("N");
        var definition = new SharedFileDefinition { StorageObjectId = objectId, SessionId = "other", AdditionalName = "../other" };

        var url = upload ? await service.GetUploadFileUrl(definition, 2) : await service.GetDownloadFileUrl(definition, 2);

        url.Should().Be("offline-signed-url");
        signed!.Key.Should().Be(objectId + "/part-2");
        signed.BucketName.Should().Be("local-test-bucket");
        signed.Verb.Should().Be(upload ? Amazon.S3.HttpVerb.PUT : Amazon.S3.HttpVerb.GET);
        signed.Expires.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
    }

}
