using System.Threading;
using Azure.Storage.Blobs;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using Serilog;
using System.IO;

namespace ByteSync.Services.Communications.Transfers;

public class FileDownloader : IFileDownloader
{
    
    private readonly IPolicyFactory _policyFactory;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly IFilePartDownloadAsserter _filePartDownloadAsserter;
    private readonly IErrorManager _errorManager;
    private readonly IResourceManager _resourceManager;
    private readonly IDownloadPartsCoordinator _partsCoordinator;
    private readonly SemaphoreSlim _semaphoreSlim;

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
        IDownloadPartsCoordinator partsCoordinator)
    {
        _policyFactory = policyFactory;
        _fileTransferApiClient = fileTransferApiClient;
        _filePartDownloadAsserter = filePartDownloadAsserter;
        _errorManager = errorManager;
        _resourceManager = resourceManager;
        _partsCoordinator = partsCoordinator;
        _semaphoreSlim = new SemaphoreSlim(1, 1);
        SharedFileDefinition = sharedFileDefinition;
        DownloadTarget = downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);
        CancellationTokenSource = new CancellationTokenSource();
        MergerTask = Task.Run(async () =>
        {
            Serilog.Log.Information("[FileDownloader] Merger task started for file {FileId}", SharedFileDefinition.Id);
            while (await _partsCoordinator.MergeChannel.Reader.WaitToReadAsync())
            {
                Serilog.Log.Information("[FileDownloader] Merger task waiting for part...");
                var partToMerge = await _partsCoordinator.MergeChannel.Reader.ReadAsync();
                Serilog.Log.Information("[FileDownloader] Merger task received part {PartNumber}", partToMerge);
                Serilog.Log.Information("[FileDownloader] Merge phase: DownloadTarget HashCode={HashCode}", DownloadTarget.GetHashCode());
                try
                {
                    Serilog.Log.Information("[FileDownloader] About to merge part {PartNumber}", partToMerge);
                    await fileMerger.MergeAsync(partToMerge);
                    Serilog.Log.Information("[FileDownloader] Finished merging part {PartNumber}", partToMerge);
                    // Log file size after merging this part
                    foreach (var destination in DownloadTarget.DownloadDestinations)
                    {
                        var fileInfo = new FileInfo(destination);
                        Serilog.Log.Information("[FileDownloader] After merge: {Path}, Size: {Size} bytes", destination, fileInfo.Length);
                    }
                }
                finally
                {
                    DownloadSemaphore.Release();
                }
            }
            Serilog.Log.Information("[FileDownloader] Merger task completed for file {FileId}", SharedFileDefinition.Id);
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
        Serilog.Log.Information("[FileDownloader] WaitForFileFullyExtracted started for file {FileId}", SharedFileDefinition.Id);
        await Task.Run(async () =>
        {
            await Task.WhenAll(DownloadTasks);
            await MergerTask;

            // Log file size after download/merge
            foreach (var destination in DownloadTarget.DownloadDestinations)
            {
                var fileInfo = new FileInfo(destination);
                Serilog.Log.Information("[FileDownloader] File exists: {Path}, Size: {Size} bytes", destination, fileInfo.Length);
            }

            if (_errorManager.IsError)
            {
                throw new Exception("An error occurred while downloading file " + DownloadTarget.DownloadDestinations.JoinToString(", "));
            }
        });
        Serilog.Log.Information("[FileDownloader] WaitForFileFullyExtracted completed for file {FileId}", SharedFileDefinition.Id);
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
                    var memoryStream = new MemoryStream();
                    Serilog.Log.Information("[FileDownloader] Download phase: DownloadTarget HashCode={HashCode}", DownloadTarget.GetHashCode());
                    var options = new BlobClientOptions();
                    options.Retry.NetworkTimeout = TimeSpan.FromMinutes(20);
                    var blob = new BlobClient(new Uri(downloadUrl), options);
                    var response = await blob.DownloadToAsync(memoryStream, CancellationTokenSource.Token);
                    memoryStream.Position = 0;
                    Serilog.Log.Information("[FileDownloader] Set memoryStream.Position=0 after download for part {PartNumber}", partNumber);
                    DownloadTarget.AddOrReplaceMemoryStream(partNumber, memoryStream);
                    Serilog.Log.Information("[FileDownloader] Added MemoryStream to DownloadTarget for part {PartNumber}, Length={Length}", partNumber, memoryStream.Length);
                    return response;
                });
                if (response is { IsError: false })
                {
                    await AssertFilePartIsDownloaded(partNumber);
                    await _semaphoreSlim.WaitAsync();
                    try
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
                    finally
                    {
                        _semaphoreSlim.Release();
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

    private async Task AssertFilePartIsDownloaded(int partNumber)
    {
        var transferParameters = new TransferParameters
        {
            SessionId = SharedFileDefinition.SessionId,
            SharedFileDefinition = SharedFileDefinition,
            PartNumber = partNumber
        };
        await _filePartDownloadAsserter.AssertAsync(transferParameters);
    }
    
    public void CleanupResources()
    {
        _resourceManager.Cleanup();
    }
    
}