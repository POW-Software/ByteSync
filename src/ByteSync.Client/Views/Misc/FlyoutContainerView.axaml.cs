using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Misc;
using ReactiveUI;

namespace ByteSync.Views.Misc;

public partial class FlyoutContainerView : ReactiveUserControl<FlyoutContainerViewModel>
{
    public FlyoutContainerView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}