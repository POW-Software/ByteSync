using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ISharedFilesService
{
    Task AssertFilePartIsUploaded(SharedFileDefinition sharedFileDefinition, int partNumber, ICollection<string> recipients);

    Task AssertUploadIsFinished(SharedFileDefinition sharedFileDefinition, int totalParts, ICollection<string> recipients);

    Task AssertFilePartIsDownloaded(SharedFileDefinition sharedFileDefinition, Client client, int partNumber, StorageProvider storageProvider);
    
    // Task AssertDownloadIsFinished(SharedFileDefinition sharedFileDefinition, Client client);
    
    Task ClearSession(string sessionId);
}