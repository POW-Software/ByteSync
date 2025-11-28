using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Factories.ViewModels;

public class ContentIdentityViewModelFactory : IContentIdentityViewModelFactory
{
    private readonly ISessionService _sessionService;
    private readonly IDateAndInventoryPartsViewModelFactory _dateAndInventoryPartsViewModelFactory;
    private readonly ILocalizationService _localizationService;
    
    public ContentIdentityViewModelFactory(ISessionService sessionService,
        IDateAndInventoryPartsViewModelFactory dateAndInventoryPartsViewModelFactory, ILocalizationService localizationService)
    {
        _sessionService = sessionService;
        _dateAndInventoryPartsViewModelFactory = dateAndInventoryPartsViewModelFactory;
        _localizationService = localizationService;
    }
    
    public ContentIdentityViewModel CreateContentIdentityViewModel(ComparisonItemViewModel comparisonItemViewModel,
        ContentIdentity contentIdentity, Inventory inventory)
    {
        var result = new ContentIdentityViewModel(comparisonItemViewModel, contentIdentity, inventory, _sessionService,
            _localizationService, _dateAndInventoryPartsViewModelFactory);
        
        return result;
    }
}