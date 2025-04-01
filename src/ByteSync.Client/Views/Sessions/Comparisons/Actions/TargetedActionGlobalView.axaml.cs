using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Comparisons.Actions;

namespace ByteSync.Views.Sessions.Comparisons.Actions;

public class TargetedActionGlobalView : ReactiveUserControl<TargetedActionGlobalViewModel>
{
    public TargetedActionGlobalView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}