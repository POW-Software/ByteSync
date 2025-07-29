using System.IO;
using System.Threading;
using Autofac;
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
    private readonly IComponentContext _context;

    public FileUploaderFactory(IComponentContext context)
    {
        _context = context;
    }

    public IFileUploader Build(string fullName, SharedFileDefinition sharedFileDefinition)
    {
        // Create the slicer encrypter
        var slicerEncrypter = _context.Resolve<ISlicerEncrypter>();
        
        // Create coordination components
        var fileUploadCoordinator = new FileUploadCoordinator(_context.Resolve<ILogger<FileUploadCoordinator>>());
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        
        // Create file slicer
        var fileSlicer = new FileSlicer(slicerEncrypter, fileUploadCoordinator.AvailableSlices, 
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, _context.Resolve<ILogger<FileSlicer>>());
        
        // Create file upload worker
        var policyFactory = _context.Resolve<IPolicyFactory>();
        var fileTransferApiClient = _context.Resolve<IFileTransferApiClient>();
        var fileUploadWorker = new FileUploadWorker(policyFactory, fileTransferApiClient, sharedFileDefinition,
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, 
            fileUploadCoordinator.UploadingIsFinished, _context.Resolve<ILogger<FileUploadWorker>>());
        
        // Create file part upload asserter
        var sessionService = _context.Resolve<ISessionService>();
        var filePartUploadAsserter = new FilePartUploadAsserter(fileTransferApiClient, sessionService);
        
        var fileUploader = _context.Resolve<IFileUploader>(
            new TypedParameter(typeof(string), fullName),
            new TypedParameter(typeof(MemoryStream), null),
            new TypedParameter(typeof(SharedFileDefinition), sharedFileDefinition),
            new TypedParameter(typeof(IFileUploadCoordinator), fileUploadCoordinator),
            new TypedParameter(typeof(IFileSlicer), fileSlicer),
            new TypedParameter(typeof(IFileUploadWorker), fileUploadWorker),
            new TypedParameter(typeof(IFilePartUploadAsserter), filePartUploadAsserter),
            new TypedParameter(typeof(ISlicerEncrypter), slicerEncrypter),
            new TypedParameter(typeof(SemaphoreSlim), semaphoreSlim)
        );
        
        return fileUploader;
    }

    public IFileUploader Build(MemoryStream memoryStream, SharedFileDefinition sharedFileDefinition)
    {
        // Create the slicer encrypter
        var slicerEncrypter = _context.Resolve<ISlicerEncrypter>();
        
        // Create coordination components
        var fileUploadCoordinator = new FileUploadCoordinator(_context.Resolve<ILogger<FileUploadCoordinator>>());
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        
        // Create file slicer
        var fileSlicer = new FileSlicer(slicerEncrypter, fileUploadCoordinator.AvailableSlices, 
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, _context.Resolve<ILogger<FileSlicer>>());
        
        // Create file upload worker
        var policyFactory = _context.Resolve<IPolicyFactory>();
        var fileTransferApiClient = _context.Resolve<IFileTransferApiClient>();
        var fileUploadWorker = new FileUploadWorker(policyFactory, fileTransferApiClient, sharedFileDefinition,
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, 
            fileUploadCoordinator.UploadingIsFinished, _context.Resolve<ILogger<FileUploadWorker>>());
        
        // Create file part upload asserter
        var sessionService = _context.Resolve<ISessionService>();
        var filePartUploadAsserter = new FilePartUploadAsserter(fileTransferApiClient, sessionService);
        
        var fileUploader = _context.Resolve<IFileUploader>(
            new TypedParameter(typeof(string), null),
            new TypedParameter(typeof(MemoryStream), memoryStream),
            new TypedParameter(typeof(SharedFileDefinition), sharedFileDefinition),
            new TypedParameter(typeof(IFileUploadCoordinator), fileUploadCoordinator),
            new TypedParameter(typeof(IFileSlicer), fileSlicer),
            new TypedParameter(typeof(IFileUploadWorker), fileUploadWorker),
            new TypedParameter(typeof(IFilePartUploadAsserter), filePartUploadAsserter),
            new TypedParameter(typeof(ISlicerEncrypter), slicerEncrypter),
            new TypedParameter(typeof(SemaphoreSlim), semaphoreSlim)
        );
        
        return fileUploader;
    }
}