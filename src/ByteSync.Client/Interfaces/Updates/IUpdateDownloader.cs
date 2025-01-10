using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Updates;
using ByteSync.Common.Business.Versions;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateDownloader
{
    // bool IsCancelled { get; }
    // bool IsFullyDownloaded { get; }
    // int ErrorCount { get; }
    //
    // IProgress<UpdateProgress> Progress { get; set; }
    //
    // SoftwareVersionFile SoftwareVersionFile { get; }

    Task DownloadAsync(CancellationToken cancellationToken);
    
    Task CheckDownloadAsync();
}