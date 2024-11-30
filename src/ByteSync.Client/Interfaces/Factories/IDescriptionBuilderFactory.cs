using ByteSync.Interfaces.Business.Actions;
using ByteSync.Services.Comparisons.DescriptionBuilders;

namespace ByteSync.Interfaces.Factories;

public interface IDescriptionBuilderFactory
{
    AtomicConditionDescriptionBuilder CreateAtomicConditionDescriptionBuilder();
    
    AtomicActionDescriptionBuilder CreateAtomicActionDescriptionBuilder();
    
    SynchronizationRuleDescriptionBuilder CreateSynchronizationRuleDescriptionBuilder(ISynchronizationRule synchronizationRule);

    SharedAtomicActionDescriptionBuilder CreateSharedAtomicActionDescriptionBuilder();
}