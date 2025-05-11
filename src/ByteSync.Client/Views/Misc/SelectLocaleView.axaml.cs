using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Misc;
using ReactiveUI;

namespace ByteSync.Views.Misc;

public partial class SelectLocaleView : ReactiveUserControl<SelectLocaleViewModel>
{
    public SelectLocaleView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}