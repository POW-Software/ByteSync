using ByteSync.Interfaces.Factories.ViewModels;
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

    public ContentIdentityViewModelFactory(ISessionService sessionService, IDateAndInventoryPartsViewModelFactory dateAndInventoryPartsViewModelFactory)
    {
        _sessionService = sessionService;
        _dateAndInventoryPartsViewModelFactory = dateAndInventoryPartsViewModelFactory;
    }
    
    public ContentIdentityViewModel CreateContentIdentityViewModel(ComparisonItemViewModel comparisonItemViewModel, ContentIdentity contentIdentity, Inventory inventory)
    {
        var result = new ContentIdentityViewModel(comparisonItemViewModel, contentIdentity, inventory, _sessionService, _dateAndInventoryPartsViewModelFactory);

        return result;
    }
}