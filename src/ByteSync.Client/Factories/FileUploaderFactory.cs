using System.IO;
using Autofac;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;

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
        var preparerFactory = _context.Resolve<IFileUploadPreparerFactory>();
        var processorFactory = _context.Resolve<IFileUploadProcessorFactory>();
        var fileUploadPreparer = preparerFactory.Create();
        var fileUploadProcessor = processorFactory.Create(slicerEncrypter, fullName, memoryStream, sharedFileDefinition);

        var fileUploader = _context.Resolve<IFileUploader>(
            new TypedParameter(typeof(string), fullName),
            new TypedParameter(typeof(MemoryStream), memoryStream),
            new TypedParameter(typeof(SharedFileDefinition), sharedFileDefinition),
            new TypedParameter(typeof(ISlicerEncrypter), slicerEncrypter),
            new TypedParameter(typeof(IFileUploadPreparer), fileUploadPreparer),
            new TypedParameter(typeof(IFileUploadProcessor), fileUploadProcessor)
        );
        
        return fileUploader;
    }
}