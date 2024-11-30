using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;
using Serilog;

namespace ByteSync.Services.Communications.Transfers;

[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
public class FileDownloader : IFileDownloader
{
    private readonly IPolicyFactory _policyFactory;
    private readonly IDownloadTargetBuilder _downloadTargetBuilder;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly IMergerDecrypterFactory _mergerDecrypterFactory;
    
    public FileDownloader(SharedFileDefinition sharedFileDefinition, IPolicyFactory policyFactory, IDownloadTargetBuilder downloadTargetBuilder,
        IFileTransferApiClient fileTransferApiClient, IMergerDecrypterFactory mergerDecrypterFactory)
    {
        _policyFactory = policyFactory;
        _downloadTargetBuilder = downloadTargetBuilder;
        _fileTransferApiClient = fileTransferApiClient;
        _mergerDecrypterFactory = mergerDecrypterFactory;
        
        SyncRoot = new object();

        DownloadPartsInfo = new DownloadPartsInfo();
        DownloadQueue = new BlockingCollection<int>();
        MergeChannel = Channel.CreateUnbounded<int>();

        DownloadTasks = new List<Task>();
        FilePartIsDownloadedAsserters = new List<Task>();

        CancellationTokenSource = new CancellationTokenSource();
        
        SharedFileDefinition = sharedFileDefinition;

        DownloadTarget = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);
        
        InitializeMergerDecrypters();
        MergerTask = Task.Run(MergeFile);

        var downloadTasks = Math.Min(8, Environment.ProcessorCount * 2);
        for (var i = 0; i < downloadTasks; i++)
        {
            var task = Task.Run(DownloadFile);
            
            DownloadTasks.Add(task);
        }
    }

    private object SyncRoot { get; }

    public SharedFileDefinition SharedFileDefinition { get; private set; }
    
    private static SemaphoreSlim DownloadSemaphore { get; } = new SemaphoreSlim(8);

    private DownloadPartsInfo DownloadPartsInfo { get; }

    private List<IMergerDecrypter>? MergerDecrypters { get; set; }

    private int? TotalPartsCount { get; set; }

    public DownloadTarget DownloadTarget { get; private set; }

    private BlockingCollection<int> DownloadQueue { get; }
    
    private Channel<int> MergeChannel { get; }
    
    private Task MergerTask { get; set; }
    
    private List<Task> DownloadTasks { get; }
    
    private List<Task> FilePartIsDownloadedAsserters { get; }
    
    private CancellationTokenSource CancellationTokenSource { get; }

    private bool IsError { get; set; }

    public Task AddAvailablePartAsync(int partNumber)
    {
        return Task.Run(() =>
        {
            lock (SyncRoot)
            {
                if (IsError)
                {
                    return;
                }
                
                DownloadPartsInfo.AvailableParts.Add(partNumber);
            
                var newDownloadableParts = DownloadPartsInfo.GetDownloadableParts();
                DownloadPartsInfo.SentToDownload.AddAll(newDownloadableParts);
            
                newDownloadableParts.ForEach(p => DownloadQueue.Add(p));
            
                // 06/03/2023: Avant, on travaillait sur DownloadPartsInfo.AvailableParts.Count == TotalPartsCount !?
                if (DownloadPartsInfo.SentToDownload.Count == TotalPartsCount)
                {
                    DownloadQueue.CompleteAdding();
                }
            }
        });
    }

    public async Task SetAllAvailablePartsKnownAsync(int partsCount)
    {
        await Task.Run(() => SetAllAvailablePartsKnown(partsCount));
    }
    
    public async Task WaitForFileFullyExtracted()
    {
        await Task.Run(async () =>
        {
            await Task.WhenAll(DownloadTasks);
            await Task.WhenAll(FilePartIsDownloadedAsserters);

            await MergerTask;
            
            bool isError;
            lock (SyncRoot)
            {
                isError = IsError;
            }

            if (isError)
            {
                throw new Exception("An error occurred while downloading file " + DownloadTarget.DownloadDestinations.JoinToString(", "));
            }
        });
    }

    private void SetAllAvailablePartsKnown(int partsCount)
    {
        lock (SyncRoot)
        {
            TotalPartsCount = partsCount;

            if (DownloadPartsInfo.SentToMerge.Count == TotalPartsCount)
            {
                MergeChannel.Writer.TryComplete();
            }

            // 06/03/2023: Avant, on travaillait sur DownloadPartsInfo.AvailableParts.Count == TotalPartsCount !?
            if (DownloadPartsInfo.SentToDownload.Count == TotalPartsCount)
            {
                DownloadQueue.CompleteAdding();
            }
        }
    }

    private async Task DownloadFile()
    {
        foreach (var partNumber in DownloadQueue.GetConsumingEnumerable())
        {
            var policy = _policyFactory.BuildFileDownloadPolicy();
            
            var isDownloadSuccess = false;

            try
            {
                await DownloadSemaphore.WaitAsync();

                lock (SyncRoot)
                {
                    if (IsError)
                    {
                        break;
                    }
                }
                
                var response = await policy.ExecuteAsync(async () =>
                    {
                        var transferParameters = new TransferParameters
                        {
                            SessionId = SharedFileDefinition.SessionId,
                            SharedFileDefinition = SharedFileDefinition,
                            PartNumber = partNumber
                        };
                        
                        var downloadUrl = await _fileTransferApiClient.GetDownloadFileUrl(transferParameters);

                        var memoryStream = DownloadTarget.GetMemoryStream(partNumber);

                        var options = new BlobClientOptions();
                        options.Retry.NetworkTimeout = TimeSpan.FromMinutes(20);

                        var blob = new BlobClient(new Uri(downloadUrl), options);
                        var response = await blob.DownloadToAsync(memoryStream, CancellationTokenSource.Token);

                        return response;
                    }
                );

                if (response is { IsError: false })
                {
                    AssertFilePartIsDownloaded(partNumber);

                    //if (SharedFileDefinition.SharedFileType == SharedFileTypes.FullInventory)
                    //{
                    //   throw new Exception("Test");
                    //}
                    
                    lock (SyncRoot)
                    {
                        DownloadPartsInfo.DownloadedParts.Add(partNumber);
                        var newMergeableParts = DownloadPartsInfo.GetMergeableParts();

                        foreach (var partToGiveToMerger in newMergeableParts)
                        {
                            MergeChannel.Writer.WriteAsync(partToGiveToMerger).GetAwaiter().GetResult();
                        }
                        
                        DownloadPartsInfo.SentToMerge.AddAll(newMergeableParts);
                        
                        if (DownloadPartsInfo.SentToMerge.Count == TotalPartsCount)
                        {
                            MergeChannel.Writer.TryComplete();
                        }
                    }

                    isDownloadSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DownloadFile - SharedFileType:{SharedFileType} - PartNumber:{PartNumber}", 
                    SharedFileDefinition.SharedFileType, partNumber);
            }

            if (!isDownloadSuccess)
            {
                lock (SyncRoot)
                {
                    SetOnError();
                }
            }
        }
    }

    private void SetOnError()
    {
        IsError = true;
        MergeChannel.Writer.TryComplete();
        DownloadQueue.CompleteAdding();
        
        CancellationTokenSource.Cancel();
    }

    private void AssertFilePartIsDownloaded(int partNumber)
    {
        var task = Task.Run(() =>
        {
            try
            {
                var transferParameters = new TransferParameters
                {
                    SessionId = SharedFileDefinition.SessionId,
                    SharedFileDefinition = SharedFileDefinition,
                    PartNumber = partNumber
                };
                
                _fileTransferApiClient.AssertFilePartIsDownloaded(transferParameters);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AssertFilePartIsDownloaded - SharedFileType:{SharedFileType} - PartNumber:{PartNumber}", 
                    SharedFileDefinition.SharedFileType, partNumber);
                
                lock (SyncRoot)
                {
                    SetOnError();
                }
            }
        });
        
        FilePartIsDownloadedAsserters.Add(task);
    }


    private async Task MergeFile()
    {
        while (await MergeChannel.Reader.WaitToReadAsync())
        {
            var partToMerge = await MergeChannel.Reader.ReadAsync();

            try
            {
                foreach (var mergerDecrypter in MergerDecrypters!)
                {
                    await mergerDecrypter.MergeAndDecrypt();
                }

                DownloadTarget.RemoveMemoryStream(partToMerge);

                lock (SyncRoot)
                {
                    DownloadPartsInfo.MergedParts.Add(partToMerge);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MergeFile");

                lock (SyncRoot)
                {
                    SetOnError();
                }

                break;
            }
            finally
            {
                DownloadSemaphore.Release();
            }
        }
    }
    
    private void InitializeMergerDecrypters()
    {
        if (MergerDecrypters == null)
        {
            MergerDecrypters = new List<IMergerDecrypter>();
            foreach (var localPath in DownloadTarget.DownloadDestinations)
            {
                var mergerDecrypter = _mergerDecrypterFactory.Build(localPath, DownloadTarget, CancellationTokenSource);
                MergerDecrypters.Add(mergerDecrypter);
            }
        }
    }
    
    public void Dispose()
    {
        DownloadPartsInfo.Clear();
        DownloadTarget.ClearMemoryStream();
    }
}