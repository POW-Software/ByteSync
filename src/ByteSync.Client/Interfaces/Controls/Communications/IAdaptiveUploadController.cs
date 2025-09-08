namespace ByteSync.Interfaces.Controls.Communications;

public interface IAdaptiveUploadController
{
	int CurrentChunkSizeBytes { get; }
	
	int CurrentParallelism { get; }

	// Returns the chunk size to use for the next slice
	int GetNextChunkSizeBytes();

	// Record the result of an upload attempt for a slice
	void RecordUploadResult(TimeSpan elapsed, bool isSuccess, int partNumber, int? statusCode = null, 
		Exception? exception = null, string? fileId = null, long actualBytes = -1);
}
