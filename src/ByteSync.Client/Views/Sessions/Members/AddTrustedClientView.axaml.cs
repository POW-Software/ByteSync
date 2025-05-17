using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Members;

namespace ByteSync.Views.Sessions.Members;

public partial class AddTrustedClientView : ReactiveUserControl<AddTrustedClientViewModel>
{
    public AddTrustedClientView()
    {
        InitializeComponent();
    }
}