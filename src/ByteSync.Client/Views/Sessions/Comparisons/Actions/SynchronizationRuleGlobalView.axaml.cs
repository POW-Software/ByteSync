using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Comparisons.Actions;

internal partial class SynchronizationRuleGlobalView : ReactiveUserControl<SynchronizationRuleGlobalViewModel>
{
    public SynchronizationRuleGlobalView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}