using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using ByteSync.Business.Communications.Downloading;

namespace ByteSync.Services.Communications.Transfers;

public class DownloadPartsCoordinator : IDownloadPartsCoordinator
{
    
    public DownloadPartsInfo DownloadPartsInfo { get; }
    public BlockingCollection<int> DownloadQueue { get; }
    public Channel<int> MergeChannel { get; }
    public bool AllPartsQueued { get; private set; }
    private readonly SemaphoreSlim _semaphoreSlim;

    public DownloadPartsCoordinator()
    {
        DownloadPartsInfo = new DownloadPartsInfo();
        DownloadQueue = new BlockingCollection<int>();
        MergeChannel = Channel.CreateUnbounded<int>();
        _semaphoreSlim = new SemaphoreSlim(1, 1);
    }

    public void AddAvailablePart(int partNumber)
    {
        _semaphoreSlim.Wait();
        try
        {
            DownloadPartsInfo.AvailableParts.Add(partNumber);
            var newDownloadableParts = DownloadPartsInfo.GetDownloadableParts();
            DownloadPartsInfo.SentToDownload.AddAll(newDownloadableParts);
            newDownloadableParts.ForEach(p => DownloadQueue.Add(p));
            // Only complete adding when all parts are available and total count is known
            if (DownloadPartsInfo.TotalPartsCount > 0 &&
                DownloadPartsInfo.AvailableParts.Count == DownloadPartsInfo.TotalPartsCount)
            {
                DownloadQueue.CompleteAdding();
                AllPartsQueued = true;
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public void SetAllPartsKnown(int totalParts)
    {
        _semaphoreSlim.Wait();
        try
        {
            DownloadPartsInfo.TotalPartsCount = totalParts;
            if (DownloadPartsInfo.SentToMerge.Count == DownloadPartsInfo.TotalPartsCount)
            {
                MergeChannel.Writer.TryComplete();
            }
            // If all parts are already available, complete adding now
            if (DownloadPartsInfo.TotalPartsCount > 0 &&
                DownloadPartsInfo.AvailableParts.Count == DownloadPartsInfo.TotalPartsCount)
            {
                DownloadQueue.CompleteAdding();
                AllPartsQueued = true;
            }
        }
        finally
        {
            _semaphoreSlim.Release();
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