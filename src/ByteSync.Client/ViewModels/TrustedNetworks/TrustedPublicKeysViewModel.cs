using System.Collections.ObjectModel;
using ByteSync.Interfaces.Controls.Communications;
using ReactiveUI;

namespace ByteSync.ViewModels.TrustedNetworks;

public class TrustedPublicKeysViewModel : ActivableViewModelBase
{
    private readonly IPublicKeysManager _publicKeysManager;

    public TrustedPublicKeysViewModel()
    {

    }

    public TrustedPublicKeysViewModel(IPublicKeysManager publicKeysManager)
    {
        _publicKeysManager = publicKeysManager;

        TrustedPublicKeys = new ObservableCollection<TrustedPublicKeyViewModel>();

        this.WhenActivated(HandleActivation);
    }
    
    private void HandleActivation(Action<IDisposable> disposables)
    {
        Refresh();
    }
    
    public ObservableCollection<TrustedPublicKeyViewModel> TrustedPublicKeys { get; set; }

    public void Refresh()
    {
        var trustedPublicKeys = _publicKeysManager.GetTrustedPublicKeys();

        TrustedPublicKeys.Clear();
        foreach (var trustedPublicKey in trustedPublicKeys!)
        {
            var trustedPublicKeyViewModel = new TrustedPublicKeyViewModel(trustedPublicKey);
            TrustedPublicKeys.Add(trustedPublicKeyViewModel);
        }
    }
}