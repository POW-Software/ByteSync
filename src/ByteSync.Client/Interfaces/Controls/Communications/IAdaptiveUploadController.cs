namespace ByteSync.Interfaces.Controls.Communications;

public interface IAdaptiveUploadController
{
	int CurrentChunkSizeBytes { get; }
	
	int CurrentParallelism { get; }
	
	int GetNextChunkSizeBytes();
	
	void RecordUploadResult(TimeSpan elapsed, bool isSuccess, int partNumber, int? statusCode = null, 
		Exception? exception = null, string? fileId = null, long actualBytes = -1);
}
