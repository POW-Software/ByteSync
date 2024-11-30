using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IBlobUrlService
{
    Task<string> GetUploadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task<string> GetDownloadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task DeleteBlob(SharedFileDefinition sharedFileDefinition, int partNumber);
    
    Task<long?> GetBlobSize(SharedFileDefinition sharedFileDefinition, int partNumber);
}