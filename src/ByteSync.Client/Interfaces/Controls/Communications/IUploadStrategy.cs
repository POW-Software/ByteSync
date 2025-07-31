using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IUploadStrategy
{
    Task<UploadLocationResponse> UploadAsync(ILogger<FileUploadWorker> logger, FileUploaderSlice slice, FileStorageLocation storageLocation, CancellationToken cancellationToken);
}