using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ByteSync.Views.Home;

public partial class JoinCloudSessionView : UserControl
{
    public JoinCloudSessionView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}