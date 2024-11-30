using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationDataLogger
{
    Task LogSentSynchronizationData(SharedSynchronizationStartData sharedSynchronizationStartData);

    Task LogReceivedSynchronizationData(SharedSynchronizationStartData sharedSynchronizationStartData);
}