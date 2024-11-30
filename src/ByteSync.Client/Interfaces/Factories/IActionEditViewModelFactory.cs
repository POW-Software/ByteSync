using ByteSync.Business.Actions.Local;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;

namespace ByteSync.Interfaces.Factories;

public interface IActionEditViewModelFactory
{
    AtomicActionEditViewModel BuildAtomicActionEditViewModel(FileSystemTypes fileSystemType, bool showDeleteButton,
        List<ComparisonItem>? comparisonItems = null, AtomicAction? baseSynchronizationAction = null);
    
    AtomicConditionEditViewModel BuildAtomicConditionEditViewModel(FileSystemTypes fileSystemType);
}