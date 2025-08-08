using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

public interface ICloudflareR2UrlService
{
    Task<string> GetUploadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task<string> GetDownloadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task DeleteObject(SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task<long?> GetObjectSize(SharedFileDefinition sharedFileDefinition, int partNumber);
} 