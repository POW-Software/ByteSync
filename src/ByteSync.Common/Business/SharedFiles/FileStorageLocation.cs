namespace ByteSync.Common.Business.SharedFiles;

public enum StorageProvider
{
    AzureBlobStorage,
    CloudflareR2
}

public record FileStorageLocation(string Url, StorageProvider StorageProvider);
