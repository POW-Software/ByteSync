using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IUploadProgressMonitor
{
    Task<long> MonitorProgressAsync(
        SharedFileDefinition sharedFileDefinition,
        UploadProgressState progressState,
        IUploadParallelismManager parallelismManager,
        ManualResetEvent finishedEvent,
        ManualResetEvent errorEvent,
        SemaphoreSlim stateSemaphore);
}