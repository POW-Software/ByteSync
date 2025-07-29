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

public class FileUploaderFactory : IFileUploaderFactory
{
    private readonly ISessionService _sessionService;
    private readonly ISlicerEncrypterFactory _slicerEncrypterFactory;
    private readonly IPolicyFactory _policyFactory;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ILogger<FileUploadCoordinator> _loggerFileUploadCoordinator;
    private readonly ILogger<FileSlicer> _loggerFileSlicer;
    private readonly ILogger<FileUploadWorker> _loggerFileUploadWorker;
    private readonly ILogger<FileUploader> _loggerFileUploader;

    public FileUploaderFactory(ISessionService sessionService, ISlicerEncrypterFactory slicerEncrypterFactory,
        IPolicyFactory policyFactory, IFileTransferApiClient fileTransferApiClient, 
        ILogger<FileUploadCoordinator> loggerFileUploadCoordinator, ILogger<FileSlicer> loggerFileSlicer,
        ILogger<FileUploadWorker> loggerFileUploadWorker, ILogger<FileUploader> loggerFileUploader)
    {
        _sessionService = sessionService;
        _slicerEncrypterFactory = slicerEncrypterFactory;
        _policyFactory = policyFactory;
        _fileTransferApiClient = fileTransferApiClient;
        _loggerFileUploadCoordinator = loggerFileUploadCoordinator;
        _loggerFileSlicer = loggerFileSlicer;
        _loggerFileUploadWorker = loggerFileUploadWorker;
        _loggerFileUploader = loggerFileUploader;
    }

    public IFileUploader Build(string fullName, SharedFileDefinition sharedFileDefinition)
    {
        // Create the slicer encrypter
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
        
        return new FileUploader(
            fullName, 
            null, 
            sharedFileDefinition, 
            fileUploadCoordinator, 
            fileSlicer, 
            fileUploadWorker, 
            filePartUploadAsserter, 
            slicerEncrypter, 
            _loggerFileUploader,
            semaphoreSlim);
    }

    public IFileUploader Build(MemoryStream memoryStream, SharedFileDefinition sharedFileDefinition)
    {
        // Create the slicer encrypter
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
        
        return new FileUploader(
            null, 
            memoryStream, 
            sharedFileDefinition, 
            fileUploadCoordinator, 
            fileSlicer, 
            fileUploadWorker, 
            filePartUploadAsserter, 
            slicerEncrypter, 
            _loggerFileUploader,
            semaphoreSlim);
    }
}