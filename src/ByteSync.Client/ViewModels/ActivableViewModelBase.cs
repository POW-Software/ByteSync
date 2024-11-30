using ReactiveUI;

namespace ByteSync.ViewModels;

public abstract class ActivableViewModelBase : ViewModelBase, IActivatableViewModel
{
    protected ActivableViewModelBase()
    {
        Activator = new ViewModelActivator();
    }
    
    public ViewModelActivator Activator { get; }
}