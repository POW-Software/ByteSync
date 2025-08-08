using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

public interface IAzureBlobStorageContainerService
{
    Task<BlobContainerClient> BuildBlobContainerClient();
    
    StorageSharedKeyCredential StorageSharedKeyCredential { get; }

    // Standardized operations akin to CloudflareR2Service
    Task<AsyncPageable<BlobItem>> ListObjectsAsync(CancellationToken cancellationToken);
    Task DeleteObjectAsync(string blobName, CancellationToken cancellationToken);
}