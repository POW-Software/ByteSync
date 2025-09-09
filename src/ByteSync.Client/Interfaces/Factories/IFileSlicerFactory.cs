using System.Threading;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Interfaces.Factories;

public interface IFileSlicerFactory
{
    public IFileSlicer Create(ISlicerEncrypter slicerEncrypter,
        Channel<FileUploaderSlice> availableSlices,
        SemaphoreSlim semaphoreSlim,
        ManualResetEvent exceptionOccurred);
}