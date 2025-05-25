using Avalonia.ReactiveUI;
using ByteSync.ViewModels.AccountDetails;
using ReactiveUI;

namespace ByteSync.Views.AccountDetails;

public partial class AccountDetailsView : ReactiveUserControl<AccountDetailsViewModel>
{
    public AccountDetailsView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables => { });
    }
}