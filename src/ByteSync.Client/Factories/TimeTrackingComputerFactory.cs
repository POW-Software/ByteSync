using Autofac.Features.Indexed;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Factories;

public class TimeTrackingComputerFactory : ITimeTrackingComputerFactory
{
    private readonly IIndex<TimeTrackingComputerType, IDataTrackingStrategy> _strategies;
    private readonly Func<IDataTrackingStrategy, ITimeTrackingComputer> _computerFactory;

    public TimeTrackingComputerFactory(
        IIndex<TimeTrackingComputerType, IDataTrackingStrategy> strategies,
        Func<IDataTrackingStrategy, ITimeTrackingComputer> computerFactory)
    {
        _strategies = strategies;
        _computerFactory = computerFactory;
    }

    public ITimeTrackingComputer Create(TimeTrackingComputerType type)
    {
        if (_strategies.TryGetValue(type, out var strategy))
        {
            return _computerFactory(strategy);
        }

        throw new ArgumentException($"No strategy found for type {type}", nameof(type));
    }
}