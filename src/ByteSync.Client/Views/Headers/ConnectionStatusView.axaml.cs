using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Headers;

namespace ByteSync.Views.Headers;

public class ConnectionStatusView : ReactiveUserControl<ConnectionStatusViewModel>
{
    public ConnectionStatusView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}