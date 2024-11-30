using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationLooper
{
    // Task InitializeData();

    Task CloudSessionSynchronizationLoop();
}