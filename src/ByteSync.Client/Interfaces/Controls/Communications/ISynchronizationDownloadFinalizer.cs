using System.Threading.Tasks;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface ISynchronizationDownloadFinalizer
{
    Task FinalizeSynchronization(SharedFileDefinition sharedFileDefinition, DownloadTarget downloadTarget);
}