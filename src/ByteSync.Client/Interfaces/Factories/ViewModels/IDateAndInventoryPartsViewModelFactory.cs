using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface IDateAndInventoryPartsViewModelFactory
{
    DateAndInventoryPartsViewModel CreateDateAndInventoryPartsViewModel(ContentIdentityViewModel contentIdentityViewModel, DateTime toLocalTime,
        HashSet<InventoryPart> inventoryPartsOK);
}