using System.Threading.Tasks;
using ByteSync.Business.Misc;

namespace ByteSync.Interfaces.Controls.TimeTracking;

public interface ITimeTrackingCache
{
    Task<ITimeTrackingComputer> GetTimeTrackingComputer(string sessionId, TimeTrackingComputerType timeTrackingComputerType);
}