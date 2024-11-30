using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationStarter
{
    Task StartSynchronization(bool isLaunchedByUser);
}