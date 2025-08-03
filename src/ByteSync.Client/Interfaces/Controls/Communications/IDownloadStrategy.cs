using System.IO;
using System.Threading;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IDownloadStrategy
{
    Task<DownloadFileResponse> DownloadAsync(Stream memoryStream, FileStorageLocation storageLocation, CancellationToken cancellationToken);
} 