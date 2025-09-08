using System.Threading;
using System.Threading.Channels;
using Autofac;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Factories;

public class FileSlicerFactory : IFileSlicerFactory
{
    private readonly IComponentContext _context;

    public FileSlicerFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public IFileSlicer Create(ISlicerEncrypter slicerEncrypter, 
        Channel<FileUploaderSlice> availableSlices, 
        SemaphoreSlim semaphoreSlim,
        ManualResetEvent exceptionOccurred)
    {
        var fileUploadProcessor = _context.Resolve<IFileSlicer>(
            new TypedParameter(typeof(ISlicerEncrypter), slicerEncrypter),
            new TypedParameter(typeof(Channel<FileUploaderSlice>), availableSlices),
            new TypedParameter(typeof(SemaphoreSlim), semaphoreSlim),
            new TypedParameter(typeof(ManualResetEvent), exceptionOccurred)
        );
        
        return fileUploadProcessor;
    }
}