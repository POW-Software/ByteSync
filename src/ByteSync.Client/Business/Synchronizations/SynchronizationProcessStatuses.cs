namespace ByteSync.Business.Synchronizations;

public enum SynchronizationProcessStatuses
{
    Pending = 0,
    SynchronizationDataReady = 1,
    Running = 2,
    Cancelled = 3,
    NotLaunched = 4,
    Error = 5,
    Success = 6,
}