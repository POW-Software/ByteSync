using System.IO;
using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FileUploader : IFileUploader
{
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly ILogger<FileUploader> _logger;
    private readonly IFileUploadPreparer _fileUploadPreparer;
    private readonly IFileUploadProcessor _fileUploadProcessor;

    public FileUploader(
        string? localFileToUpload, 
        MemoryStream? memoryStream, 
        SharedFileDefinition sharedFileDefinition, 
        IFileUploadCoordinator fileUploadCoordinator,
        IFileSlicer fileSlicer, 
        IFileUploadWorker fileUploadWorker, 
        IFilePartUploadAsserter filePartUploadAsserter,
        ISlicerEncrypter slicerEncrypter, 
        ILogger<FileUploader> logger,
        SemaphoreSlim semaphoreSlim)
    {
        if (localFileToUpload == null && memoryStream == null)
        {
            throw new ApplicationException("localFileToUpload and memoryStream are null");
        }
        
        _slicerEncrypter = slicerEncrypter;
        _logger = logger;
        
        LocalFileToUpload = localFileToUpload;
        MemoryStream = memoryStream;
        SharedFileDefinition = sharedFileDefinition ?? throw new NullReferenceException("SharedFileDefinition is null");

        // Create the separate components
        _fileUploadPreparer = new FileUploadPreparer();
        _fileUploadProcessor = new FileUploadProcessor(
            slicerEncrypter,
            logger,
            fileUploadCoordinator,
            fileSlicer,
            fileUploadWorker,
            filePartUploadAsserter,
            localFileToUpload,
            semaphoreSlim);
    }

    public int? MaxSliceLength { get; set; }
    private SharedFileDefinition SharedFileDefinition { get; set; }
    private string? LocalFileToUpload { get; set; }
    private MemoryStream? MemoryStream { get; set; }

#if DEBUG
    public Action<FileUploaderSlice?>? DebugAfterSliceMethod { get; set; }
    public Action<SharedFileDefinition, FileUploaderSlice>? DebugAfterUploadMethod { get; set; }
#endif

    public Task Upload()
    {
        if (LocalFileToUpload != null)
        {
            return UploadFile();
        }
        else
        {
            return UploadMemoryStream();
        }
    }

    private async Task UploadFile()
    {
        var fileInfo = new FileInfo(LocalFileToUpload!);
        
        _fileUploadPreparer.PrepareUpload(SharedFileDefinition, fileInfo.Length);
        
        _logger.LogInformation("FileUploader: Starting the E2EE upload of {SharedFileDefinitionId} from {File} ({length} KB)", 
            SharedFileDefinition.Id, LocalFileToUpload, SharedFileDefinition.UploadedFileLength / 1024d);

        _slicerEncrypter.Initialize(fileInfo, SharedFileDefinition);
        
        await _fileUploadProcessor.ProcessUpload(SharedFileDefinition, MaxSliceLength);
    }

    private async Task UploadMemoryStream()
    {
        _fileUploadPreparer.PrepareUpload(SharedFileDefinition, MemoryStream!.Length);
        
        _logger.LogInformation("FileUploader: Starting the E2EE upload of {SharedFileDefinitionId} from Memory ({length} KB)", 
            SharedFileDefinition.Id, SharedFileDefinition.UploadedFileLength / 1024d);
        
        _slicerEncrypter.Initialize(MemoryStream!, SharedFileDefinition);
        
        await _fileUploadProcessor.ProcessUpload(SharedFileDefinition, MaxSliceLength);
    }
}