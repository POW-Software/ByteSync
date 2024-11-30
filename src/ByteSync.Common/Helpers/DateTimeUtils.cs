using System;

namespace ByteSync.Common.Helpers;

public static class DateTimeUtils
{
    public static DateTime Trim(this DateTime date, long ticks)
    {
        // https://stackoverflow.com/questions/21704604/have-datetime-now-return-to-the-nearest-second
        return new DateTime(date.Ticks - (date.Ticks % ticks), date.Kind);
    }
        
    public static DateTimeOffset Trim(this DateTimeOffset date, long ticks)
    {
        date = date.AddTicks(-(date.Ticks % ticks));
            
        return date;
    }

    public static TimeSpan StripMilliseconds(this TimeSpan time)
    {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    
    public static bool IsOlderThan(this DateTime date, TimeSpan timeSpan)
    {
        if (timeSpan < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSpan), "timespan can not be negative");
        }
        
        var difference = DateTime.Now - date;
            
        bool result = difference > timeSpan;

        return result;
    }
    
    public static bool IsOlderThan(this DateTimeOffset date, TimeSpan timeSpan)
    {
        if (timeSpan < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSpan), "timespan can not be negative");
        }
        
        var difference = DateTimeOffset.Now - date;
            
        bool result = difference > timeSpan;

        return result;
    }
}