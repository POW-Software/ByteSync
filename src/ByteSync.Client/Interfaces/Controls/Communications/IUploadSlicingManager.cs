using System.Threading;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IUploadSlicingManager
{
    Task<UploadProgressState> Enqueue(
        SharedFileDefinition sharedFileDefinition,
        ISlicerEncrypter slicerEncrypter,
        Channel<FileUploaderSlice> availableSlices,
        SemaphoreSlim semaphoreSlim,
        ManualResetEvent exceptionOccurred);
}