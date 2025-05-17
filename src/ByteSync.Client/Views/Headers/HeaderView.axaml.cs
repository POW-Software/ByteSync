using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Headers;
using ReactiveUI;

namespace ByteSync.Views.Headers;

public partial class HeaderView : ReactiveUserControl<HeaderViewModel>
{
    public HeaderView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}