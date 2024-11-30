using ByteSync.Interfaces;
using ByteSync.Interfaces.Business.Actions;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Comparisons.DescriptionBuilders;

namespace ByteSync.Business.Comparisons;

public class DescriptionBuilderFactory : IDescriptionBuilderFactory
{
    private readonly ILocalizationService _localizationService;
    private readonly ISizeUnitConverter _sizeUnitConverter;

    public DescriptionBuilderFactory(ILocalizationService localizationService, ISizeUnitConverter sizeUnitConverter)
    {
        _localizationService = localizationService;
        _sizeUnitConverter = sizeUnitConverter;
    }
    
    public AtomicConditionDescriptionBuilder CreateAtomicConditionDescriptionBuilder()
    {
        return new AtomicConditionDescriptionBuilder(_localizationService, _sizeUnitConverter);
    }

    public AtomicActionDescriptionBuilder CreateAtomicActionDescriptionBuilder()
    {
        return new AtomicActionDescriptionBuilder(_localizationService);
    }

    public SynchronizationRuleDescriptionBuilder CreateSynchronizationRuleDescriptionBuilder(ISynchronizationRule synchronizationRule)
    {
        return new SynchronizationRuleDescriptionBuilder(synchronizationRule, _localizationService, this);
    }
    
    public SharedAtomicActionDescriptionBuilder CreateSharedAtomicActionDescriptionBuilder()
    {
        return new SharedAtomicActionDescriptionBuilder(_localizationService);
    }
}