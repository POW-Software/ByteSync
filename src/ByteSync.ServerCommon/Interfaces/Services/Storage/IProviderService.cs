namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

using ByteSync.Common.Business.SharedFiles;

public interface IProviderUrlService
{
    Task<string> GetUploadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber);
    Task<string> GetDownloadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber);
    Task DeleteObject(SharedFileDefinition sharedFileDefinition, int partNumber);
    Task<long?> GetObjectSize(SharedFileDefinition sharedFileDefinition, int partNumber);
}


