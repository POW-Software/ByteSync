using Azure.Storage;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Services.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class AzureBlobStorageContainerServiceTests
{
    private AzureBlobStorageSettings _settings;
    private IOptions<AzureBlobStorageSettings> _options;
    private AzureBlobStorageContainerService _service;

    [SetUp]
    public void SetUp()
    {
        _settings = new AzureBlobStorageSettings
        {
            AccountName = "testaccount",
            AccountKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("testkey")),
            Endpoint = "https://test.blob.core.windows.net/",
            Container = "testcontainer"
        };
        _options = Options.Create(_settings);
        _service = new AzureBlobStorageContainerService(_options);
    }

    [Test]
    public void StorageSharedKeyCredential_ShouldReturnValidCredential()
    {
        // Act
        var credential = _service.StorageSharedKeyCredential;

        // Assert
        credential.Should().NotBeNull();
        credential.AccountName.Should().Be(_settings.AccountName);
        credential.Should().BeOfType<StorageSharedKeyCredential>();
    }

    [Test]
    public void StorageSharedKeyCredential_ShouldBeSingleton()
    {
        // Act
        var credential1 = _service.StorageSharedKeyCredential;
        var credential2 = _service.StorageSharedKeyCredential;

        // Assert
        credential1.Should().BeSameAs(credential2);
    }
}