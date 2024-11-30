using Azure.Storage.Blobs;

namespace ByteSync.ServerCommon.Interfaces.Storage;

public interface IBlobContainerProvider
{
    public BlobContainerClient GetBlobContainerClient();
}