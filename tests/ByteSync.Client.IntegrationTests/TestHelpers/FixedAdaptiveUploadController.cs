using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public class FixedAdaptiveUploadController : IAdaptiveUploadController
{
    public int CurrentChunkSizeBytes { get; private set; }
    public int CurrentParallelism { get; private set; }

    public FixedAdaptiveUploadController(int chunkSizeBytes, int parallelism)
    {
        CurrentChunkSizeBytes = chunkSizeBytes;
        CurrentParallelism = parallelism;
    }

    public int GetNextChunkSizeBytes()
    {
        return CurrentChunkSizeBytes;
    }

    public void RecordUploadResult(TimeSpan elapsed, bool isSuccess, int partNumber, int? statusCode = null, Exception? exception = null, string? fileId = null, long actualBytes = -1)
    {
        // no-op: fixed behavior for tests
    }
}


