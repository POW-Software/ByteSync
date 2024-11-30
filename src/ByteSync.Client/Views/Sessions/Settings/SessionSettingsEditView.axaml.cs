using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Settings;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Settings;

public class SessionSettingsEditView : ReactiveUserControl<SessionSettingsEditViewModel>
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