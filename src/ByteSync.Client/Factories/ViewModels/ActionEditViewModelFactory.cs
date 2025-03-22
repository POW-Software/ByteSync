using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;

namespace ByteSync.Factories.ViewModels;

public class ActionEditViewModelFactory : IActionEditViewModelFactory
{
    private readonly IComponentContext _context;

    public ActionEditViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public AtomicActionEditViewModel BuildAtomicActionEditViewModel(FileSystemTypes fileSystemType, bool showDeleteButton,
        AtomicAction? atomicAction = null,
        List<ComparisonItem>? comparisonItems = null)
    {
        var result = _context.Resolve<AtomicActionEditViewModel>(
            new TypedParameter(typeof(FileSystemTypes), fileSystemType),
            new TypedParameter(typeof(bool), showDeleteButton),
            new TypedParameter(typeof(List<ComparisonItem>), comparisonItems));
        
        if (atomicAction != null)
        {
            result.SetAtomicAction(atomicAction);
        }

        return result;
    }

    public AtomicConditionEditViewModel BuildAtomicConditionEditViewModel(FileSystemTypes fileSystemType, 
        AtomicCondition? atomicCondition = null)
    {
        var result = _context.Resolve<AtomicConditionEditViewModel>(
            new TypedParameter(typeof(FileSystemTypes), fileSystemType));

        if (atomicCondition != null)
        {
            result.SetAtomicCondition(atomicCondition);
        }
        
        return result;
    }
}