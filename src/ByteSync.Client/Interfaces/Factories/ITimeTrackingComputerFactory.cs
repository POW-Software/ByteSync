using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.TimeTracking;

namespace ByteSync.Interfaces.Factories;

public interface ITimeTrackingComputerFactory
{
    ITimeTrackingComputer Create(TimeTrackingComputerType type);
}