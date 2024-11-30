using System.IO;
using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface IDeltaManager
{
    Task<string> BuildDelta(SharedActionsGroup sharedActionsGroup, SharedDataPart sharedDataPart, string sourceFullName);

    Task ApplyDelta(string destinationFullName, string deltaFullName);
    
    Task ApplyDelta(string destinationFullName, Stream deltaStream);
}