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

public class FileUploadProcessorFactory : IFileUploadProcessorFactory
{
    private readonly IComponentContext _context;

    public FileUploadProcessorFactory(IComponentContext context)
    {
        _context = context;
    }

    public IFileUploadProcessor Create(
        string? localFileToUpload,
        MemoryStream? memoryStream,
        SharedFileDefinition sharedFileDefinition)
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
        var strategies = _context.Resolve<IIndex<StorageProvider, IUploadStrategy>>();
        var fileUploadWorker = new FileUploadWorker(policyFactory, fileTransferApiClient, sharedFileDefinition,
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, strategies,
            fileUploadCoordinator.UploadingIsFinished, _context.Resolve<ILogger<FileUploadWorker>>());
        
        // Create file part upload asserter
        var sessionService = _context.Resolve<ISessionService>();
        var filePartUploadAsserter = new FilePartUploadAsserter(fileTransferApiClient, sessionService);
        
        var fileUploadProcessor = _context.Resolve<IFileUploadProcessor>(
            new TypedParameter(typeof(ISlicerEncrypter), slicerEncrypter),
            new TypedParameter(typeof(IFileUploadCoordinator), fileUploadCoordinator),
            new TypedParameter(typeof(IFileSlicer), fileSlicer),
            new TypedParameter(typeof(IFileUploadWorker), fileUploadWorker),
            new TypedParameter(typeof(IFilePartUploadAsserter), filePartUploadAsserter),
            new TypedParameter(typeof(string), localFileToUpload),
            new TypedParameter(typeof(SemaphoreSlim), semaphoreSlim)
        );
        
        return fileUploadProcessor;
    }
} 