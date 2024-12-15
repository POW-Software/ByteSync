using ByteSync.Business.Actions.Local;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface ISynchronizationRuleSummaryViewModelFactory
{
    SynchronizationRuleSummaryViewModel Create(SynchronizationRule synchronizationRule);
}