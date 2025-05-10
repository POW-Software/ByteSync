using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.AccountDetails;
using ReactiveUI;

namespace ByteSync.Views.AccountDetails;

public partial class AccountDetailsView : ReactiveUserControl<AccountDetailsViewModel>
{
    public AccountDetailsView()
    {
        this.WhenActivated(disposables => { });
        
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}