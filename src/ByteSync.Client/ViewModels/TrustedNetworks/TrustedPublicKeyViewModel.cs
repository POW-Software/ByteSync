using ByteSync.Business.Communications;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.TrustedNetworks;

public class TrustedPublicKeyViewModel : ViewModelBase
{
    public TrustedPublicKeyViewModel(TrustedPublicKey trustedPublicKey)
    {
        TrustedPublicKey = trustedPublicKey;

        ClientId = TrustedPublicKey.ClientId;
        
        ValidationDateTimeUtc = TrustedPublicKey.ValidationDate;
        
        PublicKey = trustedPublicKey.PublicKeyHash;
    }

    public TrustedPublicKey TrustedPublicKey { get; }
    
    [Reactive]
    public string ClientId { get; set; }
    
    [Reactive]
    public DateTimeOffset ValidationDateTimeUtc { get; set; }
    
    [Reactive]
    public string PublicKey { get; set; }
}