namespace ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;

public class BaseAtomicEditViewModel : ViewModelBase
{
    public event EventHandler<BaseAtomicEditViewModel>? RemoveRequested;
    
    protected void RaiseRemoveRequested()
    {
        RemoveRequested?.Invoke(this, this);
    }
}