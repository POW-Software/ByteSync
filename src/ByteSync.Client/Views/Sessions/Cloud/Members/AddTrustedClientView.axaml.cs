using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Cloud.Members;

namespace ByteSync.Views.Sessions.Cloud.Members;

public partial class AddTrustedClientView : ReactiveUserControl<AddTrustedClientViewModel>
{
    public AddTrustedClientView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}