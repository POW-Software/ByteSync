using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationStartFactory
{
    Task<SharedSynchronizationStartData> PrepareSharedData();
}