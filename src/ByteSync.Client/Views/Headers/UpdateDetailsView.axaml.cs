using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Headers;

namespace ByteSync.Views.Headers;

public class UpdateDetailsView : ReactiveUserControl<UpdateDetailsViewModel>
{
    public UpdateDetailsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}