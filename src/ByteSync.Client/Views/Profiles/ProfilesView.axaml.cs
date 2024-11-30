using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Profiles;
using ReactiveUI;

namespace ByteSync.Views.Profiles;

public class ProfilesView : ReactiveUserControl<ProfilesViewModel>
{
    public ProfilesView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}