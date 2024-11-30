using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationDataLogger : ISynchronizationDataLogger
{
    private readonly ILogger<SynchronizationDataLogger> _logger;
    private readonly ISharedAtomicActionRepository _sharedAtomicActionRepository;
    private readonly ISynchronizationRulesService _synchronizationRulesService;
    private readonly IDescriptionBuilderFactory _descriptionBuilderFactory;
    private readonly IFormatKbSizeConverter _formatKbSizeConverter;

    public SynchronizationDataLogger(ISharedAtomicActionRepository sharedAtomicActionRepository, ISynchronizationRulesService synchronizationRulesService,
        IDescriptionBuilderFactory descriptionBuilderFactory, IFormatKbSizeConverter formatKbSizeConverter, 
        ILogger<SynchronizationDataLogger> logger)
    {
        _sharedAtomicActionRepository = sharedAtomicActionRepository;
        _synchronizationRulesService = synchronizationRulesService;
        _descriptionBuilderFactory = descriptionBuilderFactory;
        _formatKbSizeConverter = formatKbSizeConverter;
        _logger = logger;
    }

    public Task LogSentSynchronizationData(SharedSynchronizationStartData sharedSynchronizationStartData)
    {
        return LogReceivedSynchronizationData(sharedSynchronizationStartData);
    }

    public Task LogReceivedSynchronizationData(SharedSynchronizationStartData sharedSynchronizationStartData)
    {
        _logger.LogInformation("The Data Synchronization actions have been set:");
        var sharedAtomicActions = _sharedAtomicActionRepository.Elements.ToList();
        if (sharedAtomicActions.Count == 0)
        {
            _logger.LogInformation(" - No action to perform");
        }
        else
        {
            _logger.LogInformation(" - {Count} action(s) to perform", sharedAtomicActions.Count);
        }
        foreach (var synchronizationRule in _synchronizationRulesService.GetLooseSynchronizationRules())
        {
            var descriptionBuilder = _descriptionBuilderFactory.CreateSynchronizationRuleDescriptionBuilder(synchronizationRule);
            descriptionBuilder.BuildDescription(" | ");
            var description = $"{descriptionBuilder.Mode} [{descriptionBuilder.Conditions}] {descriptionBuilder.Then} " +
                              $"[{descriptionBuilder.Actions}]";

            _logger.LogInformation(" - Synchronization Rule: {Description}", description);
        }
        foreach (var sharedAtomicAction in sharedAtomicActions.Where(a => !a.IsFromSynchronizationRule))
        {
            var descriptionBuilder = _descriptionBuilderFactory.CreateSharedAtomicActionDescriptionBuilder();
            var description = $"{sharedAtomicAction.PathIdentity.LinkingData} ({sharedAtomicAction.PathIdentity.FileSystemType}) - " +
                              $"{descriptionBuilder.GetDescription(sharedAtomicAction)}";

            _logger.LogInformation(" - Targeted Action: {LinkingData} ({FileSystemType}) - {Description}",
                sharedAtomicAction.PathIdentity.LinkingData, sharedAtomicAction.PathIdentity.FileSystemType, description);
        }
        _logger.LogInformation("Total volume to process: {TotalVolumeToProcess}",
            _formatKbSizeConverter.Convert(sharedSynchronizationStartData.TotalVolumeToProcess));
        
        return Task.CompletedTask;
    }
}