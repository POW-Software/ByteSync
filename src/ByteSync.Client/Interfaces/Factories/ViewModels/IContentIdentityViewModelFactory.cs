using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface IContentIdentityViewModelFactory
{
    ContentIdentityViewModel CreateContentIdentityViewModel(ComparisonItemViewModel comparisonItemViewModel, ContentIdentity contentIdentity, Inventory inventory);
}