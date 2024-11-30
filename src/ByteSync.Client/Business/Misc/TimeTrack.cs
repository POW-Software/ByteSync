using ByteSync.Common.Helpers;

namespace ByteSync.Business.Misc;

public class TimeTrack : ICloneable
{
    public DateTime? StartDateTime { get; set; }
    
    public TimeSpan ElapsedTime
    {
        get
        {
            return (DateTime.Now - StartDateTime!.Value).StripMilliseconds();
        }
    }
    
    public DateTime? EstimatedEndDateTime { get; set; }
    
    public TimeSpan? RemainingTime { get; set; }

    public void Reset(DateTime startDateTime)
    {
        StartDateTime = startDateTime;
        // ElapsedTime = TimeSpan.Zero;

        EstimatedEndDateTime = null;
        RemainingTime = null;
    }

    // public void UpdateElapsedTime()
    // {
    //     ElapsedTime = (DateTime.Now - StartDateTime!.Value).StripMilliseconds();
    // }

    public void UpdateRemainingTime()
    {
        if (EstimatedEndDateTime != null)
        {
            TimeSpan delay = EstimatedEndDateTime.Value - DateTime.Now.Trim(TimeSpan.TicksPerSecond);

            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            RemainingTime = delay.StripMilliseconds();
        }
        else
        {
            RemainingTime = null;
        }
    }

    public object Clone()
    {
        var clone = this.MemberwiseClone();

        return clone;
    }
}