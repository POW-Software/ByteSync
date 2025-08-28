using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public class TestUploadStrategy : IUploadStrategy
{
    private static readonly object Sync = new object();
    public static readonly Dictionary<string, List<(int PartNumber, long Bytes, int TaskId)>> Records = new();
    private static int _inFlight;
    private static int _maxInFlight;

    public static void Reset()
    {
        lock (Sync)
        {
            Records.Clear();
        }
        Interlocked.Exchange(ref _inFlight, 0);
        Interlocked.Exchange(ref _maxInFlight, 0);
    }

    public static int MaxInFlight => Volatile.Read(ref _maxInFlight);

    public Task<UploadFileResponse> UploadAsync(FileUploaderSlice slice, FileStorageLocation storageLocation, CancellationToken cancellationToken)
    {
        var taskId = Environment.CurrentManagedThreadId;
        return UploadRecordedAsync(slice, storageLocation, taskId, cancellationToken);
    }

    private static async Task<UploadFileResponse> UploadRecordedAsync(FileUploaderSlice slice, FileStorageLocation storageLocation, int taskId, CancellationToken cancellationToken)
    {
        var now = Interlocked.Increment(ref _inFlight);
        while (true)
        {
            var snapshot = Volatile.Read(ref _maxInFlight);
            if (now > snapshot)
            {
                Interlocked.CompareExchange(ref _maxInFlight, now, snapshot);
                if (Volatile.Read(ref _maxInFlight) >= now) break;
            }
            else break;
        }
        try
        {
            await Task.Delay(50, cancellationToken);
            lock (Sync)
            {
                var sharedId = storageLocation.Url;
                if (!Records.ContainsKey(sharedId))
                {
                    Records[sharedId] = new List<(int, long, int)>();
                }
                Records[sharedId].Add((slice.PartNumber, slice.MemoryStream?.Length ?? 0L, taskId));
            }
            return UploadFileResponse.Success(200);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }
}


