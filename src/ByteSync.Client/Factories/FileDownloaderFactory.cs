using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Communications.Transfers;
using ByteSync.Services.Communications.Transfers.Strategies;

namespace ByteSync.Factories;

public class FileDownloaderFactory : IFileDownloaderFactory
{
    private readonly IPolicyFactory _policyFactory;
    private readonly IDownloadTargetBuilder _downloadTargetBuilder;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly IMergerDecrypterFactory _mergerDecrypterFactory;
    private readonly ILogger<FilePartDownloadAsserter> _logger;
    private readonly ILogger<FileDownloader> _loggerFileDownloader;

    public FileDownloaderFactory(IPolicyFactory policyFactory, IDownloadTargetBuilder downloadTargetBuilder,
        IFileTransferApiClient fileTransferApiClient, IMergerDecrypterFactory mergerDecrypterFactory, ILogger<FilePartDownloadAsserter> logger, ILogger<FileDownloader> loggerFileDownloader)
    {
        _policyFactory = policyFactory;
        _downloadTargetBuilder = downloadTargetBuilder;
        _fileTransferApiClient = fileTransferApiClient;
        _mergerDecrypterFactory = mergerDecrypterFactory;
        _logger = logger;
        _loggerFileDownloader = loggerFileDownloader;
    }
    
    public IFileDownloader Build(SharedFileDefinition sharedFileDefinition)
    {
        // Create the parts coordinator (provides DownloadPartsInfo, DownloadQueue, MergeChannel, and syncRoot)
        var partsCoordinator = new DownloadPartsCoordinator();
        var downloadPartsInfo = partsCoordinator.DownloadPartsInfo;
        var downloadQueue = partsCoordinator.DownloadQueue;
        var mergeChannel = partsCoordinator.MergeChannel;

        var cancellationTokenSource = new System.Threading.CancellationTokenSource();

        // Build the download target
        var downloadTarget = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);

        var semaphoreSlim = new System.Threading.SemaphoreSlim(1, 1);

        // ErrorManager
        var errorManager = new ErrorManager(semaphoreSlim, mergeChannel, downloadQueue, cancellationTokenSource);

        // ResourceManager
        var resourceManager = new ResourceManager(downloadPartsInfo, downloadTarget);

        // FileMerger
        var mergerDecrypters = new List<IMergerDecrypter>();
        if (downloadTarget.TemporaryFileManagers != null && downloadTarget.TemporaryFileManagers.Count > 0)
        {
            // Synchronization (Full) with temporary files
            foreach (var tempFileManager in downloadTarget.TemporaryFileManagers)
            {
                var tempPath = tempFileManager.GetDestinationTemporaryPath();
                mergerDecrypters.Add(_mergerDecrypterFactory.Build(tempPath, downloadTarget, cancellationTokenSource));
            }
        }
        else if (downloadTarget.DownloadDestinations != null && downloadTarget.DownloadDestinations.Count > 0)
        {
            // Single file or multi-file zip (the zip path is the only destination)
            foreach (var dest in downloadTarget.DownloadDestinations)
            {
                mergerDecrypters.Add(_mergerDecrypterFactory.Build(dest, downloadTarget, cancellationTokenSource));
            }
        }
        
        var fileMerger = new FileMerger(mergerDecrypters, errorManager, downloadTarget, semaphoreSlim);
        // FilePartDownloadAsserter
        var filePartDownloadAsserter = new FilePartDownloadAsserter(_fileTransferApiClient, semaphoreSlim, errorManager, _logger);
        
        // DownloadStrategyFactory
        var downloadStrategyFactory = new DownloadStrategyFactory();

        return new FileDownloader(
            sharedFileDefinition,
            _policyFactory,
            _downloadTargetBuilder,
            _fileTransferApiClient,
            filePartDownloadAsserter,
            fileMerger,
            errorManager,
            resourceManager,
            partsCoordinator,
            downloadStrategyFactory,
            _loggerFileDownloader
        );
    }
}