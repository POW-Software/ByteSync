using System.Collections.Concurrent;
using System.Threading.Channels;
using ByteSync.Business.Communications.Downloading;

namespace ByteSync.Services.Communications.Transfers;

public class DownloadPartsCoordinator : IDownloadPartsCoordinator
{
    
    public DownloadPartsInfo DownloadPartsInfo { get; }
    public BlockingCollection<int> DownloadQueue { get; }
    public Channel<int> MergeChannel { get; }
    public bool AllPartsQueued { get; private set; }
    private readonly object _syncRoot;

    public DownloadPartsCoordinator()
    {
        DownloadPartsInfo = new DownloadPartsInfo();
        DownloadQueue = new BlockingCollection<int>();
        MergeChannel = Channel.CreateUnbounded<int>();
        _syncRoot = new object();
    }

    public void AddAvailablePart(int partNumber)
    {
        lock (_syncRoot)
        {
            DownloadPartsInfo.AvailableParts.Add(partNumber);
            var newDownloadableParts = DownloadPartsInfo.GetDownloadableParts();
            DownloadPartsInfo.SentToDownload.AddAll(newDownloadableParts);
            newDownloadableParts.ForEach(p => DownloadQueue.Add(p));
            if (DownloadPartsInfo.SentToDownload.Count == DownloadPartsInfo.TotalPartsCount)
            {
                DownloadQueue.CompleteAdding();
                AllPartsQueued = true;
            }
        }
    }

    public void SetAllPartsKnown(int totalParts)
    {
        lock (_syncRoot)
        {
            DownloadPartsInfo.TotalPartsCount = totalParts;
            if (DownloadPartsInfo.SentToMerge.Count == DownloadPartsInfo.TotalPartsCount)
            {
                MergeChannel.Writer.TryComplete();
            }
            if (DownloadPartsInfo.SentToDownload.Count == DownloadPartsInfo.TotalPartsCount)
            {
                DownloadQueue.CompleteAdding();
                AllPartsQueued = true;
            }
        }
    }

    public async Task AddAvailablePartAsync(int partNumber)
    {
        await Task.Run(() => AddAvailablePart(partNumber));
    }

    public async Task SetAllPartsKnownAsync(int totalParts)
    {
        await Task.Run(() => SetAllPartsKnown(totalParts));
    }
    
} 