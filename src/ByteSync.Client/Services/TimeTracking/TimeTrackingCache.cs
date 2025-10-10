using System.Threading;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Services.TimeTracking;

public class TimeTrackingCache : ITimeTrackingCache
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ITimeTrackingComputerFactory _timeTrackingComputerFactory;
    
    public TimeTrackingCache(ITimeTrackingComputerFactory timeTrackingComputerFactory)
    {
        _timeTrackingComputerFactory = timeTrackingComputerFactory;
        TimeTimeComputers = new Dictionary<string, ITimeTrackingComputer>();
    }
    
    private Dictionary<string, ITimeTrackingComputer> TimeTimeComputers { get; }
    
    public async Task<ITimeTrackingComputer> GetTimeTrackingComputer(string sessionId, TimeTrackingComputerType timeTrackingComputerType)
    {
        await _semaphore.WaitAsync();
        try
        {
            var key = BuildKey(sessionId, timeTrackingComputerType);
            
            if (!TimeTimeComputers.TryGetValue(key, out var timeTrackingComputer))
            {
                timeTrackingComputer = _timeTrackingComputerFactory.Create(timeTrackingComputerType);
                TimeTimeComputers.Add(key, timeTrackingComputer);
            }
            
            return timeTrackingComputer;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private string BuildKey(string sessionId, TimeTrackingComputerType timeTrackingComputerType)
    {
        return $"{sessionId}_{timeTrackingComputerType}";
    }
}