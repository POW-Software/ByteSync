using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Local;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Local;

public partial class LocalSessionPartsView : ReactiveUserControl<LocalSessionPartsViewModel>
{
    public LocalSessionPartsView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}