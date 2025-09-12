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
        ISlicerEncrypter slicerEncrypter,
        string? localFileToUpload,
        MemoryStream? memoryStream,
        SharedFileDefinition sharedFileDefinition)
    {
        var fileUploadCoordinator = new FileUploadCoordinator(_context.Resolve<ILogger<FileUploadCoordinator>>());
        var semaphoreSlim = new SemaphoreSlim(1, 1);

        var adaptiveUploadController = _context.Resolve<IAdaptiveUploadController>();

        var initialSlots = Math.Min(Math.Max(1, adaptiveUploadController.CurrentParallelism), 4);
        var uploadSlotsLimiter = new SemaphoreSlim(initialSlots, 4);

        var policyFactory = _context.Resolve<IPolicyFactory>();
        var fileTransferApiClient = _context.Resolve<IFileTransferApiClient>();
        var strategies = _context.Resolve<IIndex<StorageProvider, IUploadStrategy>>();
        var fileUploadWorker = new FileUploadWorker(policyFactory, fileTransferApiClient, sharedFileDefinition,
            semaphoreSlim, fileUploadCoordinator.ExceptionOccurred, strategies,
            fileUploadCoordinator.UploadingIsFinished, _context.Resolve<ILogger<FileUploadWorker>>(), adaptiveUploadController,
            uploadSlotsLimiter);

        var slicingManager = _context.Resolve<IUploadSlicingManager>();
        var fileUploadProcessor = _context.Resolve<IFileUploadProcessor>(
            new TypedParameter(typeof(ISlicerEncrypter), slicerEncrypter),
            new TypedParameter(typeof(IFileUploadCoordinator), fileUploadCoordinator),
            new TypedParameter(typeof(IFileUploadWorker), fileUploadWorker),
            new TypedParameter(typeof(IFileTransferApiClient), fileTransferApiClient),
            new TypedParameter(typeof(ISessionService), _context.Resolve<ISessionService>()),
            new TypedParameter(typeof(string), localFileToUpload),
            new TypedParameter(typeof(SemaphoreSlim), semaphoreSlim),
            new TypedParameter(typeof(IAdaptiveUploadController), adaptiveUploadController),
            new TypedParameter(typeof(IUploadSlicingManager), slicingManager),
            new NamedParameter("uploadSlotsLimiter", uploadSlotsLimiter)
        );

        return fileUploadProcessor;
    }
}