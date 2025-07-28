using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ITransferLocationService
{
    Task<string> GetUploadFileUrl(string sessionId, Client client,
        SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task<string> GetDownloadFileUrl(string sessionId, Client client,
        SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task AssertFilePartIsUploaded(string sessionId, Client client, TransferParameters transferParameters);
    
    Task AssertUploadIsFinished(string sessionId, Client client, TransferParameters transferParameters);
    
    Task AssertFilePartIsDownloaded(string sessionId, Client client, SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task AssertDownloadIsFinished(string sessionId, Client client, SharedFileDefinition sharedFileDefinition);
    
    FileStorageLocation CreateResponseObject(string url, StorageProvider storageProvider);
}