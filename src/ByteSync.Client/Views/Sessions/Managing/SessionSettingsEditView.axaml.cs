using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Managing;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Managing;

public partial class SessionSettingsEditView : ReactiveUserControl<SessionSettingsEditViewModel>
{
    public SessionSettingsEditView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}