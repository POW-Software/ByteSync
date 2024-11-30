using System.Threading.Tasks;
using ByteSync.Business.Communications;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationDataReceiver
{
    Task OnSynchronizationDataFileDownloaded(LocalSharedFile downloadTargetLocalSharedFile);
}