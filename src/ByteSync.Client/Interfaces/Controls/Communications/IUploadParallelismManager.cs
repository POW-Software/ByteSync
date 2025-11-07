using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IUploadParallelismManager
{
    void StartInitialWorkers(int count, Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState);
    
    void EnsureWorkers(int desired, Channel<FileUploaderSlice> availableSlices, UploadProgressState progressState);
    
    void AdjustParallelism(int desired);
    
    int GetDesiredParallelism();
    
    void SetGrantedSlots(int slots);
    
    int StartedWorkersCount { get; }
}