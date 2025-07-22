using System.IO;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Factories;

public class FileUploaderFactory : IFileUploaderFactory
{
    private readonly ISessionService _sessionService;
    private readonly ISlicerEncrypter _slicerEncrypter;
    private readonly IPolicyFactory _policyFactory;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ILoggerFactory _loggerFactory;

    public FileUploaderFactory(ISessionService sessionService, ISlicerEncrypter slicerEncrypter,
        IPolicyFactory policyFactory, IFileTransferApiClient fileTransferApiClient, ILoggerFactory loggerFactory)
    {
        _sessionService = sessionService;
        _slicerEncrypter = slicerEncrypter;
        _policyFactory = policyFactory;
        _fileTransferApiClient = fileTransferApiClient;
        _loggerFactory = loggerFactory;
    }

    public IFileUploader Build(string fullName, SharedFileDefinition sharedFileDefinition)
    {
        // Create coordination components
        var fileUploadCoordinator = new FileUploadCoordinator(_loggerFactory.CreateLogger<FileUploadCoordinator>());
        var fileSlicer = new FileSlicer(_slicerEncrypter, fileUploadCoordinator.AvailableSlices, 
            fileUploadCoordinator.SyncRoot, fileUploadCoordinator.ExceptionOccurred, 
            _loggerFactory.CreateLogger<FileSlicer>());
        var fileUploadWorker = new FileUploadWorker(_policyFactory, _fileTransferApiClient, sharedFileDefinition,
            fileUploadCoordinator.SyncRoot, fileUploadCoordinator.ExceptionOccurred, 
            fileUploadCoordinator.UploadingIsFinished, _loggerFactory.CreateLogger<FileUploadWorker>());
        var filePartUploadAsserter = new FilePartUploadAsserter(_fileTransferApiClient, _sessionService, 
            _loggerFactory.CreateLogger<FilePartUploadAsserter>());
        
        return new FileUploader(fullName, null, sharedFileDefinition, fileUploadCoordinator, fileSlicer, 
            fileUploadWorker, filePartUploadAsserter, _slicerEncrypter, _loggerFactory.CreateLogger<FileUploader>());
    }

    public IFileUploader Build(MemoryStream memoryStream, SharedFileDefinition sharedFileDefinition)
    {
        // Create coordination components
        var fileUploadCoordinator = new FileUploadCoordinator(_loggerFactory.CreateLogger<FileUploadCoordinator>());
        var fileSlicer = new FileSlicer(_slicerEncrypter, fileUploadCoordinator.AvailableSlices, 
            fileUploadCoordinator.SyncRoot, fileUploadCoordinator.ExceptionOccurred, 
            _loggerFactory.CreateLogger<FileSlicer>());
        var fileUploadWorker = new FileUploadWorker(_policyFactory, _fileTransferApiClient, sharedFileDefinition,
            fileUploadCoordinator.SyncRoot, fileUploadCoordinator.ExceptionOccurred, 
            fileUploadCoordinator.UploadingIsFinished, _loggerFactory.CreateLogger<FileUploadWorker>());
        var filePartUploadAsserter = new FilePartUploadAsserter(_fileTransferApiClient, _sessionService, 
            _loggerFactory.CreateLogger<FilePartUploadAsserter>());
        
        return new FileUploader(
            null, 
            memoryStream, 
            sharedFileDefinition, 
            fileUploadCoordinator, 
            fileSlicer, 
            fileUploadWorker, 
            filePartUploadAsserter, 
            _slicerEncrypter, 
            _loggerFactory.CreateLogger<FileUploader>());
    }
}