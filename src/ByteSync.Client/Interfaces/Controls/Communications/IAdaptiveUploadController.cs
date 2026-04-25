using ByteSync.Common.Business.Communications.Transfers;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IAdaptiveUploadController
{
	int CurrentChunkSizeBytes { get; }
	
	int CurrentParallelism { get; }
	
	int GetNextChunkSizeBytes();
	
	void RecordUploadResult(UploadResult uploadResult);
}
