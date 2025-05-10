using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Misc;
using ReactiveUI;

namespace ByteSync.Views.Misc;

public partial class SelectLocaleView : ReactiveUserControl<SelectLocaleViewModel>
{
    public SelectLocaleView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}