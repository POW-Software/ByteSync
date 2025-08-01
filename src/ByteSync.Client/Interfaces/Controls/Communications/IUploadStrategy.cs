using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IUploadStrategy
{
    Task<UploadFileResponse> UploadAsync(FileUploaderSlice slice, FileStorageLocation storageLocation, CancellationToken cancellationToken);
}