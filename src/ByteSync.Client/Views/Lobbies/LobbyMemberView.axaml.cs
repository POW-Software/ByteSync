using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Lobbies;
using ReactiveUI;

namespace ByteSync.Views.Lobbies;

public partial class LobbyMemberView : ReactiveUserControl<LobbyMemberViewModel>
{
    public LobbyMemberView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}