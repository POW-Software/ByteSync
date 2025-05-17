using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Lobbies;
using ReactiveUI;

namespace ByteSync.Views.Lobbies;

public partial class LobbyMainView : ReactiveUserControl<LobbyMainViewModel>
{
    public LobbyMainView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}