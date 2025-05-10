using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Headers;

namespace ByteSync.Views.Headers;

public partial class AboutApplicationView : ReactiveUserControl<AboutApplicationViewModel>
{
    public AboutApplicationView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}