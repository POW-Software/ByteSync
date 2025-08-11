namespace ByteSync.ServerCommon.Services.Storage.Factories;

using Azure.Storage;
using Azure.Storage.Blobs;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using Microsoft.Extensions.Options;

public class AzureBlobContainerClientFactory : IAzureBlobContainerClientFactory
{
    private readonly AzureBlobStorageSettings _settings;
    private BlobContainerClient? _containerClient;
    private StorageSharedKeyCredential? _storageSharedKeyCredential;

    public AzureBlobContainerClientFactory(IOptions<AzureBlobStorageSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<BlobContainerClient> GetOrCreateContainer(CancellationToken cancellationToken)
    {
        if (_containerClient == null)
        {
            string endpoint = _settings.Endpoint.TrimEnd('/');
            string container = _settings.Container.TrimStart('/').TrimEnd('/') + "/";
            Uri baseUri = new Uri(endpoint);
            Uri fullUri = new Uri(baseUri, container);

            _containerClient = new BlobContainerClient(fullUri, GetCredential());
            await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }

        return _containerClient;
    }

    public StorageSharedKeyCredential GetCredential()
    {
        return _storageSharedKeyCredential ??= new StorageSharedKeyCredential(_settings.AccountName, _settings.AccountKey);
    }
}


