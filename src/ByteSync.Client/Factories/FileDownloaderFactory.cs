using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Factories;

public class FileDownloaderFactory : IFileDownloaderFactory
{
    private readonly IPolicyFactory _policyFactory;
    private readonly IDownloadTargetBuilder _downloadTargetBuilder;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly IMergerDecrypterFactory _mergerDecrypterFactory;

    public FileDownloaderFactory(IPolicyFactory policyFactory, IDownloadTargetBuilder downloadTargetBuilder,
        IFileTransferApiClient fileTransferApiClient, IMergerDecrypterFactory mergerDecrypterFactory)
    {
        _policyFactory = policyFactory;
        _downloadTargetBuilder = downloadTargetBuilder;
        _fileTransferApiClient = fileTransferApiClient;
        _mergerDecrypterFactory = mergerDecrypterFactory;
    }
    
    public IFileDownloader Build(SharedFileDefinition sharedFileDefinition)
    {
        // Create the parts coordinator (provides DownloadPartsInfo, DownloadQueue, MergeChannel, and syncRoot)
        var partsCoordinator = new DownloadPartsCoordinator();
        var downloadPartsInfo = partsCoordinator.DownloadPartsInfo;
        var downloadQueue = partsCoordinator.DownloadQueue;
        var mergeChannel = partsCoordinator.MergeChannel;
        var syncRoot = typeof(DownloadPartsCoordinator)
            .GetField("_syncRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(partsCoordinator) ?? partsCoordinator; // fallback to coordinator if private field not found

        var cancellationTokenSource = new System.Threading.CancellationTokenSource();

        // Build the download target
        var downloadTarget = _downloadTargetBuilder.BuildDownloadTarget(sharedFileDefinition);

        // ErrorManager
        var errorManager = new ErrorManager(syncRoot, mergeChannel, downloadQueue, cancellationTokenSource);

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
        // else: no files to merge (should not happen)
        Action<int> onPartMerged = partNumber => { /* Optionally update state here */ };
        Action onError = () => errorManager.SetOnError();
        Action<int> removeMemoryStream = partNumber => downloadTarget.RemoveMemoryStream(partNumber);
        var fileMerger = new FileMerger(mergerDecrypters, onPartMerged, onError, removeMemoryStream, syncRoot);

        // FilePartDownloadAsserter
        var filePartDownloadAsserter = new FilePartDownloadAsserter(_fileTransferApiClient, syncRoot, onError);

        return new FileDownloader(
            sharedFileDefinition,
            _policyFactory,
            _downloadTargetBuilder,
            _fileTransferApiClient,
            _mergerDecrypterFactory,
            filePartDownloadAsserter,
            fileMerger,
            errorManager,
            resourceManager,
            partsCoordinator
        );
    }
}