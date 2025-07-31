using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IDownloadStrategy
{
    Task<Response> DownloadAsync(Stream memoryStream, FileStorageLocation storageLocation, CancellationToken cancellationToken);
} 