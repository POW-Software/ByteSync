using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business.Misc;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.TimeTracking;

namespace ByteSync.Services.TimeTracking;

class TimeTrackingComputer : ITimeTrackingComputer
{
    private readonly BehaviorSubject<TimeTrack> _timeTrack;
    private readonly BehaviorSubject<bool> _isStarted;
    
    private readonly IDataTrackingStrategy _dataTrackingStrategy;
    
    public TimeTrackingComputer(IDataTrackingStrategy dataTrackingStrategy)
    {
        _dataTrackingStrategy = dataTrackingStrategy;
            
        _timeTrack = new BehaviorSubject<TimeTrack>(new TimeTrack());
        _isStarted = new BehaviorSubject<bool>(false);

        TotalDataToHandle = 0;
        DataHandled = 0;
            
        _isStarted.CombineLatest(_dataTrackingStrategy.GetDataObservable())
            .Where(tuple => tuple.First)
            .Subscribe(tuple =>
            {
                var timeTracking = (TimeTrack) _timeTrack.Value.Clone();

                var (_, data) = tuple;
                SetDataToHandle(data.IdentifiedSize);
                SetDataHandled(data.ProcessedSize);

                UpdateTimeTracking(timeTracking);

                _timeTrack.OnNext(timeTracking);
            });
    }

    public DateTime? LastDataHandledDateTime { get; private set; }
    
    public IObservable<TimeTrack> RemainingTime
    {
        get
        {
            return _isStarted
                .Select(isStarted => {
                    if (isStarted) {
                        return Observable.Interval(TimeSpan.FromSeconds(1));
                    } else {
                        return Observable.Empty<long>();
                    }
                })
                .Switch() 
                .CombineLatest(_timeTrack, (_, timeTracking) => timeTracking)
                .AsObservable();
        }
    }

    private long TotalDataToHandle { get; set; }

    private long DataHandled { get; set; }
        
    public void Start(DateTimeOffset startDateTime)
    {
        TimeTrack timeTrack;
        
        TotalDataToHandle = 0;
        DataHandled = 0;
        LastDataHandledDateTime = null;
            
        timeTrack = new TimeTrack();
        timeTrack.Reset(startDateTime.LocalDateTime);
        
        UpdateTimeTracking(timeTrack);
        
        _timeTrack.OnNext(timeTrack);
        _isStarted.OnNext(true);
    }

    public void Stop()
    {
        _isStarted.OnNext(false);
        
        var timeTrack = (TimeTrack) _timeTrack.Value.Clone();
                    
        if (timeTrack.StartDateTime != null)
        {
            var end = timeTrack.StartDateTime.Value.Trim(TimeSpan.TicksPerSecond) + timeTrack.ElapsedTime;
            timeTrack.EstimatedEndDateTime = end.Trim(TimeSpan.TicksPerSecond);
        }
                    
        timeTrack.RemainingTime = TimeSpan.Zero;
                    
        _timeTrack.OnNext(timeTrack);
    }

    private void SetDataToHandle(long dataToHandle)
    {
        TotalDataToHandle = dataToHandle;
    }

    private void SetDataHandled(long dataHandled)
    {
        DataHandled = dataHandled;
        LastDataHandledDateTime = DateTime.Now;
    }

    private void UpdateTimeTracking(TimeTrack timeTrack)
    {
        DateTime? estimatedEndDateTime = null;
            
        if (timeTrack.StartDateTime != null && LastDataHandledDateTime != null)
        {
            if (TotalDataToHandle != 0 && DataHandled != 0 && DataHandled <= TotalDataToHandle)
            {
                double percent = ((double) DataHandled) / TotalDataToHandle;

                var differenceTicks = (LastDataHandledDateTime.Value - timeTrack.StartDateTime.Value).Ticks;

                if (differenceTicks > 0)
                {
                    var endTicks = differenceTicks / percent;

                    estimatedEndDateTime = timeTrack.StartDateTime.Value.AddTicks((long) endTicks);
                }
            }
        }

        timeTrack.EstimatedEndDateTime = estimatedEndDateTime;
        timeTrack.UpdateRemainingTime();
        
        _timeTrack.OnNext(timeTrack);
    }
}