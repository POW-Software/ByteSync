using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFileUploadWorker
{
	Task UploadAvailableSlicesAdaptiveAsync(Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState);
} 