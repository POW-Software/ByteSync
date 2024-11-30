using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationActionHandler
{
    Task RunSynchronizationAction(SharedActionsGroup sharedActionsGroup);
    
    Task RunPendingSynchronizationActions();
}