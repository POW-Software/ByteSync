using Azure.Storage;
using Azure.Storage.Blobs;

namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

public interface IBlobStorageContainerService
{
    Task<BlobContainerClient> BuildBlobContainerClient();
    
    StorageSharedKeyCredential StorageSharedKeyCredential { get; }
}