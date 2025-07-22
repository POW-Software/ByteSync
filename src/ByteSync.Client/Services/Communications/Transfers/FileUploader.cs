using System.IO;
using System.Security.Cryptography;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers;

public class FileUploader : IFileUploader
{
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly ILogger<FileUploader> _logger;

    // Coordination components
    private readonly IFileUploadCoordinator _fileUploadCoordinator;
    private readonly IFileSlicer _fileSlicer;
    private readonly IFileUploadWorker _fileUploadWorker;
    private readonly IFilePartUploadAsserter _filePartUploadAsserter;

    // State tracking
    private UploadProgressState? _progressState;

    public FileUploader(
        string? localFileToUpload, 
        MemoryStream? memoryStream, 
        SharedFileDefinition sharedFileDefinition, 
        IFileUploadCoordinator fileUploadCoordinator,
        IFileSlicer fileSlicer, 
        IFileUploadWorker fileUploadWorker, 
        IFilePartUploadAsserter filePartUploadAsserter,
        ISlicerEncrypter slicerEncrypter, 
        ILogger<FileUploader> logger)
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

        _fileUploadCoordinator = fileUploadCoordinator;
        _fileSlicer = fileSlicer;
        _fileUploadWorker = fileUploadWorker;
        _filePartUploadAsserter = filePartUploadAsserter;
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
        
        PrepareUpload(SharedFileDefinition, fileInfo.Length);
        
        _logger.LogInformation("FileUploader: Starting the E2EE upload of {SharedFileDefinitionId} from {File} ({length} KB)", 
            SharedFileDefinition.Id, LocalFileToUpload, SharedFileDefinition.UploadedFileLength / 1024d);

        _slicerEncrypter.Initialize(fileInfo, SharedFileDefinition);
        
        await ProcessUpload(SharedFileDefinition);
    }

    private async Task UploadMemoryStream()
    {
        PrepareUpload(SharedFileDefinition, MemoryStream!.Length);
        
        _logger.LogInformation("FileUploader: Starting the E2EE upload of {SharedFileDefinitionId} from Memory ({length} KB)", 
            SharedFileDefinition.Id, SharedFileDefinition.UploadedFileLength / 1024d);
        
        _slicerEncrypter.Initialize(MemoryStream!, SharedFileDefinition);
        
        await ProcessUpload(SharedFileDefinition);
    }

    private void PrepareUpload(SharedFileDefinition sharedFileDefinition, long length)
    {
        using (var aes = Aes.Create())
        {
            aes.GenerateIV();
            sharedFileDefinition.IV = aes.IV;
        }
        
        SharedFileDefinition = sharedFileDefinition;
        sharedFileDefinition.UploadedFileLength = length;
    }
    
    private async Task ProcessUpload(SharedFileDefinition sharedFileDefinition)
    {
        _progressState = new UploadProgressState();
        
        // Start upload workers
        for (var i = 0; i < 6; i++)
        {
            _ = Task.Run(() => _fileUploadWorker.UploadAvailableSlicesAsync(_fileUploadCoordinator.AvailableSlices, _progressState));
        }
        
        // Start slicer
        await Task.Run(() => _fileSlicer.SliceAndEncryptAsync(sharedFileDefinition, _progressState, 
            MaxSliceLength));
        
        // Wait for completion
        await _fileUploadCoordinator.WaitForCompletionAsync();

        _slicerEncrypter.Dispose();

        if (_progressState.LastException != null)
        {
            var source = LocalFileToUpload ?? "a stream";
            throw new Exception($"An error occured while uploading '{source}' / sharedFileDefinition.Id:{sharedFileDefinition.Id}",
                _progressState.LastException);
        }

        var totalCreatedSlices = GetTotalCreatedSlices();
        await _filePartUploadAsserter.AssertUploadIsFinished(sharedFileDefinition, totalCreatedSlices);
        
        _logger.LogInformation("FileUploader: E2EE upload of {SharedFileDefinitionId} is finished", SharedFileDefinition.Id);
    }

    public int GetTotalCreatedSlices()
    {
        lock (_fileUploadCoordinator.SyncRoot)
        {
            return _progressState?.TotalCreatedSlices ?? 0;
        }
    }
    
    public int GetMaxConcurrentUploads()
    {
        lock (_fileUploadCoordinator.SyncRoot)
        {
            return _progressState?.MaxConcurrentUploads ?? 0;
        }
    }
}