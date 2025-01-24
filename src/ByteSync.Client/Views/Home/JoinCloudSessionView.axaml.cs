using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Home;

namespace ByteSync.Views.Home;

public class JoinCloudSessionView : ReactiveUserControl<JoinCloudSessionViewModel>
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