using ByteSync.Business.Actions.Local;
using ByteSync.Business.Actions.Loose;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Interfaces.Controls.Profiles;

public interface ISynchronizationRulesConverter
{
    public bool CheckAllDataPartsAreMappable(List<LooseSynchronizationRule> looseSynchronizationRules);

    public List<SynchronizationRuleSummaryViewModel> ConvertToSynchronizationRuleViewModels(
        List<LooseSynchronizationRule> looseSynchronizationRules);

    public List<LooseSynchronizationRule> ConvertLooseSynchronizationRules(ICollection<SynchronizationRule> synchronizationRules);
}