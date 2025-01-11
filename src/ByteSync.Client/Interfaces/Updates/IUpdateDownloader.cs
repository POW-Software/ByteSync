using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Updates;
using ByteSync.Common.Business.Versions;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateDownloader
{
    Task DownloadAsync(CancellationToken cancellationToken);
    
    Task CheckDownloadAsync();
}