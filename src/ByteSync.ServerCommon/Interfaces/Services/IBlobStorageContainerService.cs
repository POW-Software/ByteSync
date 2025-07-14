using Azure.Storage;
using Azure.Storage.Blobs;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IBlobStorageContainerService
{
    Task<BlobContainerClient> BuildBlobContainerClient();
    
    StorageSharedKeyCredential StorageSharedKeyCredential { get; }
}