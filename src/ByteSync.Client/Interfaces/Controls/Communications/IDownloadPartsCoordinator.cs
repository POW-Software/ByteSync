using System.Collections.Concurrent;
using System.Threading.Channels;
using ByteSync.Business.Communications.Downloading;

namespace ByteSync.Services.Communications.Transfers;

public interface IDownloadPartsCoordinator
{
    void AddAvailablePart(int partNumber);
    void SetAllPartsKnown(int totalParts);
    bool AllPartsQueued { get; }
    BlockingCollection<int> DownloadQueue { get; }
    Channel<int> MergeChannel { get; }
    DownloadPartsInfo DownloadPartsInfo { get; }
    Task AddAvailablePartAsync(int partNumber);
    Task SetAllPartsKnownAsync(int totalParts);
} 