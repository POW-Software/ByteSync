using ReactiveUI;

namespace ByteSync.ViewModels;

public abstract class ActivatableViewModelBase : ViewModelBase, IActivatableViewModel
{
    protected ActivatableViewModelBase()
    {
        Activator = new ViewModelActivator();
    }
    
    public ViewModelActivator Activator { get; }
}