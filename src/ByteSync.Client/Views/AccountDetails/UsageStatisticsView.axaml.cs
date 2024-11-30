using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.AccountDetails;
using ReactiveUI;

namespace ByteSync.Views.AccountDetails;

public partial class UsageStatisticsView : ReactiveUserControl<UsageStatisticsViewModel>
{
    public UsageStatisticsView()
    {
        this.WhenActivated(disposables => { });
        
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}