using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationActionRemoteUploader
{
    Task UploadForRemote(SharedActionsGroup sharedActionsGroup);
    
    Task Complete();
    
    Task Abort();
}