using ByteSync.Business.Actions.Loose;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Lobbies;
using ByteSync.ViewModels.Lobbies;
using CloudSessionProfileDetails = ByteSync.Business.Profiles.CloudSessionProfileDetails;

namespace ByteSync.Services.Lobbies;

public class LobbySynchronizationRuleViewModelFactory : ILobbySynchronizationRuleViewModelFactory
{
    private readonly ILocalizationService _localizationService;
    private readonly IDescriptionBuilderFactory _descriptionBuilderFactory;

    public LobbySynchronizationRuleViewModelFactory(ILocalizationService localizationService, IDescriptionBuilderFactory descriptionBuilderFactory)
    {
        _localizationService = localizationService;
        _descriptionBuilderFactory = descriptionBuilderFactory;
    }
    
    public LobbySynchronizationRuleViewModel Create(LooseSynchronizationRule synchronizationRule, CloudSessionProfileDetails profileDetails)
    {
        var isVisible = profileDetails.Options.Settings.DataType == DataTypes.FilesDirectories;
        
        var result = new LobbySynchronizationRuleViewModel(synchronizationRule, isVisible, _localizationService, _descriptionBuilderFactory);

        return result;
    }

    public LobbySynchronizationRuleViewModel Create(LooseSynchronizationRule synchronizationRule, bool isVisible)
    {
        var result = new LobbySynchronizationRuleViewModel(synchronizationRule, isVisible, _localizationService, _descriptionBuilderFactory);

        return result;
    }
}