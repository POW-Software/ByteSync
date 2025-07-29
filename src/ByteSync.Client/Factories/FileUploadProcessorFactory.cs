using System.IO;
using System.Threading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Factories;

public class FileUploadProcessorFactory : IFileUploadProcessorFactory
{
    private readonly ISlicerEncrypterFactory _slicerEncrypterFactory;
    private readonly IPolicyFactory _policyFactory;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ISessionService _sessionService;
    private readonly ILogger<FileUploadCoordinator> _loggerFileUploadCoordinator;
    private readonly ILogger<FileSlicer> _loggerFileSlicer;
    private readonly ILogger<FileUploadWorker> _loggerFileUploadWorker;
    private readonly ILogger<FileUploadProcessor> _loggerFileUploadProcessor;

    public FileUploadProcessorFactory(
        ISlicerEncrypterFactory slicerEncrypterFactory,
        IPolicyFactory policyFactory,
        IFileTransferApiClient fileTransferApiClient,
        ISessionService sessionService,
        ILogger<FileUploadCoordinator> loggerFileUploadCoordinator,
        ILogger<FileSlicer> loggerFileSlicer,
        ILogger<FileUploadWorker> loggerFileUploadWorker,
        ILogger<FileUploadProcessor> loggerFileUploadProcessor)
    {
        _slicerEncrypterFactory = slicerEncrypterFactory;
        _policyFactory = policyFactory;
        _fileTransferApiClient = fileTransferApiClient;
        _sessionService = sessionService;
        _loggerFileUploadCoordinator = loggerFileUploadCoordinator;
        _loggerFileSlicer = loggerFileSlicer;
        _loggerFileUploadWorker = loggerFileUploadWorker;
        _loggerFileUploadProcessor = loggerFileUploadProcessor;
    }

    public IFileUploadProcessor Create(
        string? localFileToUpload,
        MemoryStream? memoryStream,
        SharedFileDefinition sharedFileDefinition)
    {
        // Create a new SlicerEncrypter instance for this upload
        var slicerEncrypter = _slicerEncrypterFactory.Create();
        
        // Create coordination components
        var fileUploadCoordinator = new FileUploadCoordinator(_loggerFileUploadCoordinator);
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        
        // Create file slicer
        var fileSlicer = new FileSlicer(slicerEncrypter, fileUploadCoordinator.AvailableSlices, 
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, _loggerFileSlicer);
        
        // Create file upload worker
        var fileUploadWorker = new FileUploadWorker(_policyFactory, _fileTransferApiClient, sharedFileDefinition,
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, 
            fileUploadCoordinator.UploadingIsFinished, _loggerFileUploadWorker);
        
        // Create file part upload asserter
        var filePartUploadAsserter = new FilePartUploadAsserter(_fileTransferApiClient, _sessionService);
        
        return new FileUploadProcessor(
            slicerEncrypter,
            _loggerFileUploadProcessor,
            fileUploadCoordinator,
            fileSlicer,
            fileUploadWorker,
            filePartUploadAsserter,
            localFileToUpload,
            semaphoreSlim);
    }
} 