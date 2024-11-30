using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Interfaces.Factories.ViewModels;

public interface IComparisonItemViewModelFactory
{
    ComparisonItemViewModel Create(ComparisonItem comparisonItem);
}