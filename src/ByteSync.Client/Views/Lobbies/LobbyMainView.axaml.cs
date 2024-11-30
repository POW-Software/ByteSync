using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Lobbies;
using ReactiveUI;

namespace ByteSync.Views.Lobbies;

public class LobbyMainView : ReactiveUserControl<LobbyMainViewModel>
{
    public LobbyMainView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}