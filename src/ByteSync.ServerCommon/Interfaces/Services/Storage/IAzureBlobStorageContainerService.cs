using Azure.Storage;
using Azure.Storage.Blobs;

namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

public interface IAzureBlobStorageContainerService
{
    Task<BlobContainerClient> BuildBlobContainerClient();
    
    StorageSharedKeyCredential StorageSharedKeyCredential { get; }
}