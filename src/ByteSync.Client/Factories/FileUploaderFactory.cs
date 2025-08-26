using System.IO;
using System.Threading;
using Autofac;
using Autofac.Features.Indexed;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers.Uploading;

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
        return DoBuild(fullName, null, sharedFileDefinition);
    }

    public IFileUploader Build(MemoryStream memoryStream, SharedFileDefinition sharedFileDefinition)
    {
        return DoBuild(null, memoryStream, sharedFileDefinition);
    }
    
    private IFileUploader DoBuild(string? fullName, MemoryStream? memoryStream, SharedFileDefinition sharedFileDefinition)
    {
        var slicerEncrypter = _context.Resolve<ISlicerEncrypter>();
        
        var fileUploadCoordinator = new FileUploadCoordinator(_context.Resolve<ILogger<FileUploadCoordinator>>());
        var semaphoreSlim = new SemaphoreSlim(1, 1);
        
        var fileSlicer = new FileSlicer(slicerEncrypter, fileUploadCoordinator.AvailableSlices, 
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, _context.Resolve<ILogger<FileSlicer>>());
        
        var policyFactory = _context.Resolve<IPolicyFactory>();
        var fileTransferApiClient = _context.Resolve<IFileTransferApiClient>();
        var strategies = _context.Resolve<IIndex<StorageProvider, IUploadStrategy>>();
        var fileUploadWorker = new FileUploadWorker(policyFactory, fileTransferApiClient, sharedFileDefinition,
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, strategies,
            fileUploadCoordinator.UploadingIsFinished, _context.Resolve<ILogger<FileUploadWorker>>());
        
        var sessionService = _context.Resolve<ISessionService>();
        var filePartUploadAsserter = new FilePartUploadAsserter(fileTransferApiClient, sessionService);
        var adaptiveUploadController = _context.Resolve<IAdaptiveUploadController>();
        
        var fileUploader = _context.Resolve<IFileUploader>(
            new TypedParameter(typeof(string), fullName),
            new TypedParameter(typeof(MemoryStream), memoryStream),
            new TypedParameter(typeof(SharedFileDefinition), sharedFileDefinition),
            new TypedParameter(typeof(IFileUploadCoordinator), fileUploadCoordinator),
            new TypedParameter(typeof(IFileSlicer), fileSlicer),
            new TypedParameter(typeof(IFileUploadWorker), fileUploadWorker),
            new TypedParameter(typeof(IFilePartUploadAsserter), filePartUploadAsserter),
            new TypedParameter(typeof(ISlicerEncrypter), slicerEncrypter),
            new TypedParameter(typeof(SemaphoreSlim), semaphoreSlim),
            new TypedParameter(typeof(IAdaptiveUploadController), adaptiveUploadController)
        );
        
        return fileUploader;
    }
}