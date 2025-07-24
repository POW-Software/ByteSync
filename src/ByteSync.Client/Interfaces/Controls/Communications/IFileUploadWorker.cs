using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileUploadWorker
{
    Task UploadAvailableSlicesAsync(Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState);
    
    void StartUploadWorkers(Channel<FileUploaderSlice> availableSlices, int workerCount, UploadProgressState progressState);
} 