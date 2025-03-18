using System.Threading;
using Autofac;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Factories;

public class MergerDecrypterFactory : IMergerDecrypterFactory
{
    private readonly IComponentContext _context;

    public MergerDecrypterFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public IMergerDecrypter Build(string localPath, DownloadTarget downloadTarget, CancellationTokenSource cancellationTokenSource)
    {
        var result = _context.Resolve<IMergerDecrypter>(
            new TypedParameter(typeof(string), localPath),
            new TypedParameter(typeof(DownloadTarget), downloadTarget),
            new TypedParameter(typeof(CancellationTokenSource), cancellationTokenSource));
        
        return result;
    }
}