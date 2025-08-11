namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

using ByteSync.Common.Business.SharedFiles;
using System.Collections.Generic;

public interface IProviderService
{
    Task<string> GetUploadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber);
    Task<string> GetDownloadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber);
    Task DeleteObject(SharedFileDefinition sharedFileDefinition, int partNumber);

    Task<IReadOnlyCollection<KeyValuePair<string, DateTimeOffset?>>> GetAllObjects(CancellationToken cancellationToken);
    Task DeleteObjectByKey(string key, CancellationToken cancellationToken);
}