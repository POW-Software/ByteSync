using Azure.Storage;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using ByteSync.ServerCommon.Services.Storage.Factories;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class AzureBlobStorageContainerServiceTests
{
    private AzureBlobStorageSettings _settings;
    private IOptions<AzureBlobStorageSettings> _options;
    private IAzureBlobContainerClientFactory _factory;

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
        _factory = new AzureBlobContainerClientFactory(_options);
    }

    [Test]
    public void StorageSharedKeyCredential_ShouldReturnValidCredential()
    {
        // Act
        var credential = _factory.GetCredential();

        // Assert
        credential.Should().NotBeNull();
        credential.AccountName.Should().Be(_settings.AccountName);
        credential.Should().BeOfType<StorageSharedKeyCredential>();
    }

    [Test]
    public void StorageSharedKeyCredential_ShouldBeSingleton()
    {
        // Act
        var credential1 = _factory.GetCredential();
        var credential2 = _factory.GetCredential();

        // Assert
        credential1.Should().BeSameAs(credential2);
    }
}