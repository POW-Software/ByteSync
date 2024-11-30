using Autofac;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Factories;

public class TemporaryFileManagerFactory : ITemporaryFileManagerFactory
{
    private readonly IComponentContext _context;

    public TemporaryFileManagerFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public ITemporaryFileManager Create(string destinationFullName)
    {
        var result = _context.Resolve<ITemporaryFileManager>(
            new TypedParameter(typeof(string), destinationFullName));

        return result;
    }
}