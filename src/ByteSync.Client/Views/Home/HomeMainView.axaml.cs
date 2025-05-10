using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Home;
using ReactiveUI;

namespace ByteSync.Views.Home;

public partial class HomeMainView : ReactiveUserControl<HomeMainViewModel>
{
    public HomeMainView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}