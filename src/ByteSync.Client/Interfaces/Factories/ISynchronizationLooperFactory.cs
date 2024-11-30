using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Interfaces.Factories;

public interface ISynchronizationLooperFactory
{   
    ISynchronizationLooper CreateSynchronizationLooper();
}