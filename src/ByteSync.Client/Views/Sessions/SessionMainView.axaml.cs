using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions;
using ReactiveUI;

namespace ByteSync.Views.Sessions;

public partial class SessionMainView : ReactiveUserControl<SessionMainViewModel>
{
    public SessionMainView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }
}