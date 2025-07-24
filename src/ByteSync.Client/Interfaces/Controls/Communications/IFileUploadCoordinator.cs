using System.Threading;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileUploadCoordinator
{
    object SyncRoot { get; }
    Channel<FileUploaderSlice> AvailableSlices { get; }
    ManualResetEvent UploadingIsFinished { get; }
    ManualResetEvent ExceptionOccurred { get; }
    
    Task WaitForCompletionAsync();
    void SetException(Exception exception);
    bool HasExceptionOccurred();
    void Reset();
} 