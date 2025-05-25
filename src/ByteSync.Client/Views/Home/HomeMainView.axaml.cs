using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Home;
using ReactiveUI;

namespace ByteSync.Views.Home;

public partial class HomeMainView : ReactiveUserControl<HomeMainViewModel>
{
    public HomeMainView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}