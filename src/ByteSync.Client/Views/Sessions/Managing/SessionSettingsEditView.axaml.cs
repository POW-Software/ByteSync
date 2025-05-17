using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Managing;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Managing;

public partial class SessionSettingsEditView : ReactiveUserControl<SessionSettingsEditViewModel>
{
    public SessionSettingsEditView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}