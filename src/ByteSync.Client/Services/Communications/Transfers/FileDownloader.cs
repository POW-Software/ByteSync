using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using Azure.Storage.Blobs;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using Serilog;

namespace ByteSync.Services.Communications.Transfers;

public class FileDownloader : IFileDownloader
{
    
    private readonly IPolicyFactory _policyFactory;
    private readonly IDownloadTargetBuilder _downloadTargetBuilder;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly IFilePartDownloadAsserter _filePartDownloadAsserter;
    private readonly IFileMerger _fileMerger;
    private readonly IErrorManager _errorManager;
    private readonly IResourceManager _resourceManager;
    private readonly IDownloadPartsCoordinator _partsCoordinator;
    private readonly object SyncRoot;

    public SharedFileDefinition SharedFileDefinition { get; private set; }
    public DownloadTarget DownloadTarget { get; private set; }

    private static SemaphoreSlim DownloadSemaphore { get; } = new SemaphoreSlim(8);
    private DownloadPartsInfo DownloadPartsInfo { get; }
    private BlockingCollection<int> DownloadQueue { get; }
    private Channel<int> MergeChannel { get; }
    private Task MergerTask { get; set; }
    private List<Task> DownloadTasks { get; }
    private CancellationTokenSource CancellationTokenSource { get; }

    public FileDownloader(
        SharedFileDefinition sharedFileDefinition,
        IPolicyFactory policyFactory,
        IDownloadTargetBuilder downloadTargetBuilder,
        IFileTransferApiClient fileTransferApiClient,
        IMergerDecrypterFactory mergerDecrypterFactory,
        IFilePartDownloadAsserter filePartDownloadAsserter,
        IFileMerger fileMerger,
        IErrorManager errorManager,
        IResourceManager resourceManager,
        IDownloadPartsCoordinator partsCoordinator)
    {
        _policyFactory = policyFactory;
        _downloadTargetBuilder = downloadTargetBuilder;
        _fileTransferApiClient = fileTransferApiClient;
        _filePartDownloadAsserter = filePartDownloadAsserter;
        _fileMerger = fileMerger;
        _errorManager = errorManager;
        _resourceManager = resourceManager;
        _partsCoordinator = partsCoordinator;
        SyncRoot = new object();
        SharedFileDefinition = sharedFileDefinition;
        DownloadTarget = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);
        MergerTask = Task.Run(async () =>
        {
            while (await _partsCoordinator.MergeChannel.Reader.WaitToReadAsync())
            {
                var partToMerge = await _partsCoordinator.MergeChannel.Reader.ReadAsync();
                try
                {
                    await _fileMerger.MergeAsync(partToMerge);
                }
                finally
                {
                    DownloadSemaphore.Release();
                }
            }
        });
        var downloadTasks = Math.Min(8, Environment.ProcessorCount * 2);
        DownloadTasks = new List<Task>();
        for (var i = 0; i < downloadTasks; i++)
        {
            var task = Task.Run(DownloadFile);
            DownloadTasks.Add(task);
        }
    }

    public async Task WaitForFileFullyExtracted()
    {
        await Task.Run(async () =>
        {
            await Task.WhenAll(DownloadTasks);
            await MergerTask;
            if (_errorManager.IsError)
            {
                throw new Exception("An error occurred while downloading file " + DownloadTarget.DownloadDestinations.JoinToString(", "));
            }
        });
    }

    private async Task DownloadFile()
    {
        foreach (var partNumber in _partsCoordinator.DownloadQueue.GetConsumingEnumerable())
        {
            var policy = _policyFactory.BuildFileDownloadPolicy();
            var isDownloadSuccess = false;
            try
            {
                await DownloadSemaphore.WaitAsync();
                if (_errorManager.IsError)
                {
                    break;
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
                });
                if (response is { IsError: false })
                {
                    AssertFilePartIsDownloaded(partNumber);
                    lock (SyncRoot)
                    {
                        _partsCoordinator.DownloadPartsInfo.DownloadedParts.Add(partNumber);
                        var newMergeableParts = _partsCoordinator.DownloadPartsInfo.GetMergeableParts();
                        foreach (var partToGiveToMerger in newMergeableParts)
                        {
                            _partsCoordinator.MergeChannel.Writer.WriteAsync(partToGiveToMerger).GetAwaiter().GetResult();
                        }
                        _partsCoordinator.DownloadPartsInfo.SentToMerge.AddAll(newMergeableParts);
                        if (_partsCoordinator.DownloadPartsInfo.SentToMerge.Count == _partsCoordinator.DownloadPartsInfo.TotalPartsCount)
                        {
                            _partsCoordinator.MergeChannel.Writer.TryComplete();
                        }
                    }
                    isDownloadSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DownloadFile - SharedFileType:{SharedFileType} - PartNumber:{PartNumber}", SharedFileDefinition.SharedFileType, partNumber);
            }
            if (!isDownloadSuccess)
            {
                _errorManager.SetOnError();
            }
        }
    }

    private void AssertFilePartIsDownloaded(int partNumber)
    {
        var transferParameters = new TransferParameters
        {
            SessionId = SharedFileDefinition.SessionId,
            SharedFileDefinition = SharedFileDefinition,
            PartNumber = partNumber
        };
        var task = _filePartDownloadAsserter.AssertAsync(transferParameters);
    }
    
    public void CleanupResources()
    {
        _resourceManager.Cleanup();
    }
    
}