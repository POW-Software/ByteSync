using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Misc;
using ReactiveUI;

namespace ByteSync.Views.Misc;

public class FlyoutContainerView : ReactiveUserControl<FlyoutContainerViewModel>
{
    public FlyoutContainerView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}