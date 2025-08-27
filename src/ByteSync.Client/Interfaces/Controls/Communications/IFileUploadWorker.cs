using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileUploadWorker
{
	// TODO: remove this method and its implementation
    Task UploadAvailableSlicesAsync(Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState);
    
    // TODO: remove this method and its implementation
    void StartUploadWorkers(Channel<FileUploaderSlice> availableSlices, int workerCount, UploadProgressState progressState);

	Task UploadAvailableSlicesAdaptiveAsync(Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState);
} 