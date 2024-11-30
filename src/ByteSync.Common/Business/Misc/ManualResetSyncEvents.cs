using System.Threading;

namespace ByteSync.Common.Business.Misc;

public class ManualResetSyncEvents
{
    private const int END_EVENT = 0;
    private const int MANUAL_RESET_EVENT_1 = 1;

    /// <summary>
    /// Instancie un SyncEvents simple avec 2 autoResetEvents
    /// </summary>
    public ManualResetSyncEvents()
    {
        EndEvent = new ManualResetEvent(false);
        ManualResetEvent = new ManualResetEvent(false);

        EventArray = new WaitHandle[2];
        EventArray[END_EVENT] = EndEvent;
        EventArray[MANUAL_RESET_EVENT_1] = ManualResetEvent;
    }

    /// <summary>
    /// Signal manuel
    /// </summary>
    private EventWaitHandle ManualResetEvent { get; }

    /// <summary>
    /// Signal de fin
    /// </summary>
    public EventWaitHandle EndEvent { get; }

    public WaitHandle[] EventArray { get; }

    public bool WaitForEvent()
    {
        var waitHandleIndex = WaitHandle.WaitAny(EventArray);

        return waitHandleIndex != END_EVENT;
    }

    public void SetEvent()
    {
        ManualResetEvent.Set();
    }

    public void ResetEvent()
    {
        ManualResetEvent.Reset();
    }

    public void SetEnd()
    {
        EndEvent.Set();
    }

    public void ResetAll()
    {
        ManualResetEvent.Reset();
        EndEvent.Reset();
    }
}