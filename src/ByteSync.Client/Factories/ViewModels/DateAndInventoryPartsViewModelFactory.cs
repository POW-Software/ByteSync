using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Factories.ViewModels;

public class DateAndInventoryPartsViewModelFactory : IDateAndInventoryPartsViewModelFactory
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;

    public DateAndInventoryPartsViewModelFactory(ISessionService sessionService, ILocalizationService localizationService)
    {
        _sessionService = sessionService;
        _localizationService = localizationService;
    }
    
    public DateAndInventoryPartsViewModel CreateDateAndInventoryPartsViewModel(ContentIdentityViewModel contentIdentityViewModel, DateTime toLocalTime,
        HashSet<InventoryPart> inventoryPartsOK)
    {
        DateAndInventoryPartsViewModel result = new DateAndInventoryPartsViewModel(contentIdentityViewModel, 
            toLocalTime, inventoryPartsOK, _sessionService, _localizationService);

        return result;
    }
}