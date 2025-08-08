using System.Threading;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using System.IO;
using Autofac.Features.Indexed;

namespace ByteSync.Services.Communications.Transfers;

public class FileDownloader : IFileDownloader
{
    
    private readonly IPolicyFactory _policyFactory;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly IFilePartDownloadAsserter _filePartDownloadAsserter;
    private readonly IErrorManager _errorManager;
    private readonly IResourceManager _resourceManager;
    private readonly IDownloadPartsCoordinator _partsCoordinator;
    private readonly IIndex<StorageProvider, IDownloadStrategy> _strategies;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly ILogger<FileDownloader> _logger;

    public IDownloadPartsCoordinator PartsCoordinator => _partsCoordinator;
    public SharedFileDefinition SharedFileDefinition { get; private set; }
    public DownloadTarget DownloadTarget { get; private set; }

    private static SemaphoreSlim DownloadSemaphore { get; } = new SemaphoreSlim(8);
    private Task MergerTask { get; set; }
    private List<Task> DownloadTasks { get; }
    private CancellationTokenSource CancellationTokenSource { get; }

    public FileDownloader(
        SharedFileDefinition sharedFileDefinition,
        IPolicyFactory policyFactory,
        IDownloadTargetBuilder downloadTargetBuilder,
        IFileTransferApiClient fileTransferApiClient,
        IFilePartDownloadAsserter filePartDownloadAsserter,
        IFileMerger fileMerger,
        IErrorManager errorManager,
        IResourceManager resourceManager,
        IDownloadPartsCoordinator partsCoordinator,
        IIndex<StorageProvider, IDownloadStrategy> strategies,
        ILogger<FileDownloader> logger)
    {
        _policyFactory = policyFactory;
        _fileTransferApiClient = fileTransferApiClient;
        _filePartDownloadAsserter = filePartDownloadAsserter;
        _errorManager = errorManager;
        _resourceManager = resourceManager;
        _partsCoordinator = partsCoordinator;
        _strategies = strategies;
        _logger = logger;
        _semaphoreSlim = new SemaphoreSlim(1, 1);
        SharedFileDefinition = sharedFileDefinition;
        DownloadTarget = downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);
        CancellationTokenSource = new CancellationTokenSource();
        MergerTask = Task.Run(async () =>
        {
            while (await _partsCoordinator.MergeChannel.Reader.WaitToReadAsync())
            {
                var partToMerge = await _partsCoordinator.MergeChannel.Reader.ReadAsync();
                try
                {
                    await fileMerger.MergeAsync(partToMerge);
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

            if (await _errorManager.IsErrorAsync())
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
                if (await _errorManager.IsErrorAsync())
                {
                    break;
                }

                // Get storage location first to determine the storage provider
                var transferParameters = new TransferParameters
                {
                    SessionId = SharedFileDefinition.SessionId,
                    SharedFileDefinition = SharedFileDefinition,
                    PartNumber = partNumber
                };
                var downloadLocation = await _fileTransferApiClient.GetDownloadFileStorageLocation(transferParameters);
                var storageProvider = downloadLocation.StorageProvider;
                _logger.LogInformation("FileDownloader: Using storage provider {StorageProvider} for file {FileId} part {PartNumber}", 
                    storageProvider, SharedFileDefinition.Id, partNumber);
                
                var downloadResponse = await policy.ExecuteAsync(async () =>
                {
                    var memoryStream = new MemoryStream();
                    var downloadStrategy = _strategies[storageProvider];
                    var response = await downloadStrategy.DownloadAsync(memoryStream, downloadLocation, CancellationTokenSource.Token);
                    
                    DownloadTarget.AddOrReplaceMemoryStream(partNumber, memoryStream);
                    return response;
                });
                if (downloadResponse.IsSuccess)
                {
                    await AssertFilePartIsDownloaded(partNumber, storageProvider);
                    await _semaphoreSlim.WaitAsync();
                    try
                    {
                        _partsCoordinator.DownloadPartsInfo.DownloadedParts.Add(partNumber);
                        var newMergeableParts = _partsCoordinator.DownloadPartsInfo.GetMergeableParts();
                        foreach (var partToGiveToMerger in newMergeableParts)
                        {
                            _partsCoordinator.MergeChannel.Writer.WriteAsync(partToGiveToMerger).GetAwaiter()
                                .GetResult();
                        }

                        _partsCoordinator.DownloadPartsInfo.SentToMerge.AddAll(newMergeableParts);
                        if (_partsCoordinator.DownloadPartsInfo.SentToMerge.Count ==
                            _partsCoordinator.DownloadPartsInfo.TotalPartsCount)
                        {
                            _partsCoordinator.MergeChannel.Writer.TryComplete();
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }

                    isDownloadSuccess = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadFile - SharedFileType:{SharedFileType} - PartNumber:{PartNumber}",
                    SharedFileDefinition.SharedFileType, partNumber);
            }

            if (!isDownloadSuccess)
            {
                await _errorManager.SetOnErrorAsync();
            }
        }
    }

    private async Task AssertFilePartIsDownloaded(int partNumber, StorageProvider storageProvider)
    {
        var transferParameters = new TransferParameters
        {
            SessionId = SharedFileDefinition.SessionId,
            SharedFileDefinition = SharedFileDefinition,
            PartNumber = partNumber,
            StorageProvider = storageProvider
        };
        
        await _filePartDownloadAsserter.AssertAsync(transferParameters);
    }
    
    public void CleanupResources()
    {
        _resourceManager.Cleanup();
    }

}