using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Lobbies;
using ReactiveUI;

namespace ByteSync.Views.Lobbies;

public partial class LobbyMemberView : ReactiveUserControl<LobbyMemberViewModel>
{
    public LobbyMemberView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}