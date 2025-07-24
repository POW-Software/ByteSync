using System.IO;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Services.Communications.Transfers;

public class FileUploadProcessor : IFileUploadProcessor
{
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly ILogger _logger;
    private readonly IFileUploadCoordinator _fileUploadCoordinator;
    private readonly IFileSlicer _fileSlicer;
    private readonly IFileUploadWorker _fileUploadWorker;
    private readonly IFilePartUploadAsserter _filePartUploadAsserter;
    private readonly string? _localFileToUpload;
    private readonly MemoryStream? _memoryStream;

    // State tracking
    private UploadProgressState? _progressState;

    public FileUploadProcessor(
        ISlicerEncrypter slicerEncrypter,
        ILogger logger,
        IFileUploadCoordinator fileUploadCoordinator,
        IFileSlicer fileSlicer,
        IFileUploadWorker fileUploadWorker,
        IFilePartUploadAsserter filePartUploadAsserter,
        string? localFileToUpload,
        MemoryStream? memoryStream)
    {
        _slicerEncrypter = slicerEncrypter;
        _logger = logger;
        _fileUploadCoordinator = fileUploadCoordinator;
        _fileSlicer = fileSlicer;
        _fileUploadWorker = fileUploadWorker;
        _filePartUploadAsserter = filePartUploadAsserter;
        _localFileToUpload = localFileToUpload;
        _memoryStream = memoryStream;
    }

    public async Task ProcessUpload(SharedFileDefinition sharedFileDefinition, int? maxSliceLength = null)
    {
        _progressState = new UploadProgressState();
        
        // Start upload workers
        for (var i = 0; i < 6; i++)
        {
            _ = Task.Run(() => _fileUploadWorker.UploadAvailableSlicesAsync(_fileUploadCoordinator.AvailableSlices, _progressState));
        }
        
        // Start slicer
        await Task.Run(() => _fileSlicer.SliceAndEncryptAsync(sharedFileDefinition, _progressState, 
            maxSliceLength));
        
        // Wait for completion
        await _fileUploadCoordinator.WaitForCompletionAsync();

        _slicerEncrypter.Dispose();

        if (_progressState.LastException != null)
        {
            var source = _localFileToUpload ?? "a stream";
            throw new Exception($"An error occured while uploading '{source}' / sharedFileDefinition.Id:{sharedFileDefinition.Id}",
                _progressState.LastException);
        }

        var totalCreatedSlices = GetTotalCreatedSlices();
        await _filePartUploadAsserter.AssertUploadIsFinished(sharedFileDefinition, totalCreatedSlices);
        
        _logger.LogInformation("FileUploadProcessor: E2EE upload of {SharedFileDefinitionId} is finished", sharedFileDefinition.Id);
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