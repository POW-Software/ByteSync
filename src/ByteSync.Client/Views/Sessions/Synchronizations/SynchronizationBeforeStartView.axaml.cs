using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Synchronizations;

namespace ByteSync.Views.Sessions.Synchronizations;

public partial class SynchronizationBeforeStartView : ReactiveUserControl<SynchronizationBeforeStartViewModel>
{
    public SynchronizationBeforeStartView()
    {
        InitializeComponent();
    }
}
