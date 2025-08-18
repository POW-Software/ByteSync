using System.Threading;
using ByteSync.Business.Actions.Shared;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationActionHandler
{
    Task RunSynchronizationAction(SharedActionsGroup sharedActionsGroup, CancellationToken cancellationToken = default);
    
    Task RunPendingSynchronizationActions(CancellationToken cancellationToken = default);
}