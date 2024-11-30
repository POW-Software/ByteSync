using System.IO;
using Autofac;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
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
        var result = _context.Resolve<IFileUploader>(
            new TypedParameter(typeof(string), fullName),
            new TypedParameter(typeof(MemoryStream), null),
            new TypedParameter(typeof(SharedFileDefinition), sharedFileDefinition));

        return result;
    }

    public IFileUploader Build(MemoryStream memoryStream, SharedFileDefinition sharedFileDefinition)
    {
        var result = _context.Resolve<IFileUploader>(
            new TypedParameter(typeof(string), null),
            new TypedParameter(typeof(MemoryStream), memoryStream),
            new TypedParameter(typeof(SharedFileDefinition), sharedFileDefinition));

        return result;
    }
}