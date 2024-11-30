using ByteSync.Business.Misc;

namespace ByteSync.Interfaces.Controls.TimeTracking;

public interface ITimeTrackingComputer
{
    // void Start(DateTime startDateTime, RemainingTimeData remainingTimeData);
    IObservable<TimeTrack> RemainingTime { get; }
    
    void Start(DateTimeOffset startDateTime);
    
    // void SetDataToHandle(long dataToHandle);
    //
    // void SetDataHandled(long dataHandled);

    void Stop();
}