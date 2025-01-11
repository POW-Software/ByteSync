using System.Threading;
using System.Threading.Tasks;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateDownloader
{
    Task DownloadAsync(CancellationToken cancellationToken);
}