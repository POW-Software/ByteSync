using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ByteSync.Views.TrustedNetworks;

public partial class TrustedNetworkView : UserControl
{
    public TrustedNetworkView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}