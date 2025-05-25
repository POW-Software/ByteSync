using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Comparisons.Actions;

internal partial class SynchronizationRuleGlobalView : ReactiveUserControl<SynchronizationRuleGlobalViewModel>
{
    public SynchronizationRuleGlobalView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}