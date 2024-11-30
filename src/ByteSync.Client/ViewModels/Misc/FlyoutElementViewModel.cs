using System.Threading.Tasks;

namespace ByteSync.ViewModels.Misc;

public abstract class FlyoutElementViewModel : ActivableViewModelBase
{
    public event EventHandler? CloseFlyoutRequested;
    
    public FlyoutContainerViewModel Container { get; set; } = null!;

    public virtual Task CancelIfNeeded()
    {
        return Task.CompletedTask;
    }

    protected void RaiseCloseFlyoutRequested()
    {
        CloseFlyoutRequested?.Invoke(this, EventArgs.Empty);
    }
    
    public virtual void OnDisplayed()
    {
        
    }
}