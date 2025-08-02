using Azure.Storage;
using Azure.Storage.Blobs;
using ByteSync.ServerCommon.Interfaces.Storage;
using Microsoft.Extensions.Configuration;

namespace ByteSync.ServerCommon.Storage;

public class BlobContainerProvider : IBlobContainerProvider
{
    private readonly IConfiguration _configuration;

    public BlobContainerProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public BlobContainerClient GetBlobContainerClient()
    {
        string accountName = _configuration.GetSection("AzureBlobStorage:Account").Value!;
        string accountKey = _configuration.GetSection("AzureBlobStorage:Key").Value!;
        string url = _configuration.GetSection("AzureBlobStorage:Url").Value!;

        StorageSharedKeyCredential storageSharedKeyCredential = new StorageSharedKeyCredential(accountName, accountKey);

        var blobContainerClient = new BlobContainerClient(new Uri(url), storageSharedKeyCredential);
        
        return blobContainerClient;
    }
}