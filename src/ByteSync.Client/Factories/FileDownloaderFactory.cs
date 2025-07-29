using Autofac;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Communications.Transfers;
using Autofac.Features.Indexed;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Factories;

public class FileDownloaderFactory : IFileDownloaderFactory
{
    private readonly IComponentContext _context;
    
    public FileDownloaderFactory(IComponentContext context)
    {
        _context = context;
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
        var downloadTargetBuilder = _context.Resolve<IDownloadTargetBuilder>();
        var downloadTarget = downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);

        var semaphoreSlim = new System.Threading.SemaphoreSlim(1, 1);

        // ErrorManager
        var errorManager = new ErrorManager(semaphoreSlim, mergeChannel, downloadQueue, cancellationTokenSource);

        // ResourceManager
        var resourceManager = new ResourceManager(downloadPartsInfo, downloadTarget);

        // FileMerger
        var mergerDecrypterFactory = _context.Resolve<IMergerDecrypterFactory>();
        
        var mergerDecrypters = new List<IMergerDecrypter>();
        if (downloadTarget.TemporaryFileManagers != null && downloadTarget.TemporaryFileManagers.Count > 0)
        {
            // Synchronization (Full) with temporary files
            foreach (var tempFileManager in downloadTarget.TemporaryFileManagers)
            {
                var tempPath = tempFileManager.GetDestinationTemporaryPath();
                mergerDecrypters.Add(mergerDecrypterFactory.Build(tempPath, downloadTarget, cancellationTokenSource));
            }
        }
        else if (downloadTarget.DownloadDestinations != null && downloadTarget.DownloadDestinations.Count > 0)
        {
            // Single file or multi-file zip (the zip path is the only destination)
            foreach (var dest in downloadTarget.DownloadDestinations)
            {
                mergerDecrypters.Add(mergerDecrypterFactory.Build(dest, downloadTarget, cancellationTokenSource));
            }
        }
        
        var fileMerger = new FileMerger(mergerDecrypters, errorManager, downloadTarget, semaphoreSlim);
        
        // FilePartDownloadAsserter
        var fileTransferApiClient = _context.Resolve<IFileTransferApiClient>();
        var filePartDownloadAsserterLogger = _context.Resolve<ILogger<FilePartDownloadAsserter>>();
        var filePartDownloadAsserter = new FilePartDownloadAsserter(fileTransferApiClient, semaphoreSlim, errorManager, filePartDownloadAsserterLogger);

        var downloadStrategy = _context.Resolve<IIndex<StorageProvider, IDownloadStrategy>>();

        var fileDownloader = _context.Resolve<IFileDownloader>(
            new TypedParameter(typeof(SharedFileDefinition), sharedFileDefinition),
            new TypedParameter(typeof(IFilePartDownloadAsserter), filePartDownloadAsserter),
            new TypedParameter(typeof(IFileMerger), fileMerger),
            new TypedParameter(typeof(IErrorManager), errorManager),
            new TypedParameter(typeof(IResourceManager), resourceManager),
            new TypedParameter(typeof(IDownloadPartsCoordinator), partsCoordinator),
            new TypedParameter(typeof(IIndex<StorageProvider, IDownloadStrategy>), downloadStrategy)
        );

        return fileDownloader;
    }
}