using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;

namespace ByteSync.Factories;

public class ActionEditViewModelFactory : IActionEditViewModelFactory
{
    private readonly IComponentContext _context;

    public ActionEditViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public AtomicActionEditViewModel BuildAtomicActionEditViewModel(FileSystemTypes fileSystemType, bool showDeleteButton, 
        List<ComparisonItem>? comparisonItems, AtomicAction? baseSynchronizationAction = null)
    {
        var result = _context.Resolve<AtomicActionEditViewModel>(
            new TypedParameter(typeof(FileSystemTypes), fileSystemType),
            new TypedParameter(typeof(bool), showDeleteButton),
            new TypedParameter(typeof(List<ComparisonItem>), comparisonItems),
            new TypedParameter(typeof(AtomicAction), baseSynchronizationAction));

        return result;
    }

    public AtomicConditionEditViewModel BuildAtomicConditionEditViewModel(FileSystemTypes fileSystemType)
    {
        var result = _context.Resolve<AtomicConditionEditViewModel>(
            new TypedParameter(typeof(FileSystemTypes), fileSystemType));

        return result;
    }
}