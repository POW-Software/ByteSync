using ByteSync.Business.Actions.Local;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface ISynchronizationActionViewModelFactory
{
    SynchronizationActionViewModel CreateSynchronizationActionViewModel(AtomicAction atomicAction, ComparisonItemViewModel comparisonItemViewModel);
}