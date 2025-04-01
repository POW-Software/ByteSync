using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Views.Sessions.Comparisons.Results;

public class ManageSynchronizationRulesView : ReactiveUserControl<ManageSynchronizationRulesViewModel>
{
    public ManageSynchronizationRulesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}