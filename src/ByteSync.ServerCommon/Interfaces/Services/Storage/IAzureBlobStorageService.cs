namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

using Azure.Storage;
using Azure.Storage.Blobs;

public interface IAzureBlobStorageService : IProviderService
{
    Task<BlobContainerClient> BuildBlobContainerClient();
    
    StorageSharedKeyCredential StorageSharedKeyCredential { get; }
}