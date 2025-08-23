using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Synchronizations;

namespace ByteSync.Views.Sessions.Synchronizations;

public partial class SynchronizationStatisticsView : ReactiveUserControl<SynchronizationStatisticsViewModel>
{
    public SynchronizationStatisticsView()
    {
        InitializeComponent();
    }
}
