using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ByteSync.Views.Home;

public partial class CreateCloudSessionView : UserControl
{
    public CreateCloudSessionView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}