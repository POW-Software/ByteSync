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
    private readonly ILoggerFactory _loggerFactory;

    public FileUploadProcessorFactory(
        ISlicerEncrypterFactory slicerEncrypterFactory,
        IPolicyFactory policyFactory,
        IFileTransferApiClient fileTransferApiClient,
        ISessionService sessionService,
        ILoggerFactory loggerFactory)
    {
        _slicerEncrypterFactory = slicerEncrypterFactory;
        _policyFactory = policyFactory;
        _fileTransferApiClient = fileTransferApiClient;
        _sessionService = sessionService;
        _loggerFactory = loggerFactory;
    }

    public IFileUploadProcessor Create(
        string? localFileToUpload,
        MemoryStream? memoryStream,
        SharedFileDefinition sharedFileDefinition)
    {
        // Create a new SlicerEncrypter instance for this upload
        var slicerEncrypter = _slicerEncrypterFactory.Create();
        
        // Create coordination components
        var fileUploadCoordinator = new FileUploadCoordinator(_loggerFactory.CreateLogger<FileUploadCoordinator>());
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        var fileSlicer = new FileSlicer(slicerEncrypter, fileUploadCoordinator.AvailableSlices, 
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, 
            _loggerFactory.CreateLogger<FileSlicer>());
        var fileUploadWorker = new FileUploadWorker(_policyFactory, _fileTransferApiClient, sharedFileDefinition,
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, 
            fileUploadCoordinator.UploadingIsFinished, _loggerFactory.CreateLogger<FileUploadWorker>());
        var filePartUploadAsserter = new FilePartUploadAsserter(_fileTransferApiClient, _sessionService);
        
        return new FileUploadProcessor(
            slicerEncrypter,
            _loggerFactory.CreateLogger<FileUploadProcessor>(),
            fileUploadCoordinator,
            fileSlicer,
            fileUploadWorker,
            filePartUploadAsserter,
            localFileToUpload,
            semaphoreSlim);
    }
} 