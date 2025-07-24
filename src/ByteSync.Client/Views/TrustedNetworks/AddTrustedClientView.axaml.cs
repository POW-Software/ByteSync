using Avalonia.ReactiveUI;
using ByteSync.ViewModels.TrustedNetworks;

namespace ByteSync.Views.TrustedNetworks;

public partial class AddTrustedClientView : ReactiveUserControl<AddTrustedClientViewModel>
{
    public AddTrustedClientView()
    {
        InitializeComponent();
    }
}