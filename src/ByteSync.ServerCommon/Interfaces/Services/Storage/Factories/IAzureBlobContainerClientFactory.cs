namespace ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;

public interface IAzureBlobContainerClientFactory
{
    Task<BlobContainerClient> GetOrCreateContainer(CancellationToken cancellationToken);
    StorageSharedKeyCredential GetCredential();
}