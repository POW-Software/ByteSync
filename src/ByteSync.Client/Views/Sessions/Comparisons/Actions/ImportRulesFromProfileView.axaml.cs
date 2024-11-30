using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Comparisons.Actions;

public partial class ImportRulesFromProfileView : ReactiveUserControl<ImportRulesFromProfileViewModel>
{
    public ImportRulesFromProfileView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}