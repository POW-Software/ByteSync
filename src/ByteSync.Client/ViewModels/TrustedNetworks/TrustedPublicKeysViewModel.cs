using System.Collections.ObjectModel;
using ByteSync.Interfaces.Controls.Communications;
using ReactiveUI;

namespace ByteSync.ViewModels.TrustedNetworks;

public class TrustedPublicKeysViewModel : ActivatableViewModelBase
{
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ILogger<TrustedPublicKeysViewModel> _logger;

    public TrustedPublicKeysViewModel()
    {

    }

    public TrustedPublicKeysViewModel(IPublicKeysManager publicKeysManager, ILogger<TrustedPublicKeysViewModel> logger)
    {
        _publicKeysManager = publicKeysManager;
        _logger = logger;

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
        try
        {
            var trustedPublicKeys = _publicKeysManager.GetTrustedPublicKeys();

            TrustedPublicKeys.Clear();
            foreach (var trustedPublicKey in trustedPublicKeys!)
            {
                var trustedPublicKeyViewModel = new TrustedPublicKeyViewModel(trustedPublicKey);
                TrustedPublicKeys.Add(trustedPublicKeyViewModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh trusted public keys");
        }
    }
}