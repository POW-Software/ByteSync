namespace ByteSync.Common.Business.SharedFiles;

public enum StorageProvider
{
    AzureBlobStorage,
    CloudFlareR2
}

public record FileStorageLocation(string Url,  StorageProvider StorageProvider);
