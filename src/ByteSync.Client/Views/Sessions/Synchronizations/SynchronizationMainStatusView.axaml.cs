using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Synchronizations;

namespace ByteSync.Views.Sessions.Synchronizations;

public partial class SynchronizationMainStatusView : ReactiveUserControl<SynchronizationMainStatusViewModel>
{
    public SynchronizationMainStatusView()
    {
        InitializeComponent();
    }
}
