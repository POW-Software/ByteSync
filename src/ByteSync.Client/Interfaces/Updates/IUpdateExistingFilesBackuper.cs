using System.Threading;
using System.Threading.Tasks;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateExistingFilesBackuper
{
    List<Tuple<string, string>> BackedUpFileSystemInfos { get; }
    
    public Task BackupExistingFilesAsync(CancellationToken cancellationToken);
}