using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Profiles;
using ReactiveUI;

namespace ByteSync.Views.Profiles;

public partial class ProfilesView : ReactiveUserControl<ProfilesViewModel>
{
    public ProfilesView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}