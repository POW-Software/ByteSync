using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Synchronizations;

public class SynchronizationMainViewModel : ActivatableViewModelBase
{
    public SynchronizationMainViewModel()
    {
    }

    public SynchronizationMainViewModel(SynchronizationBeforeStartViewModel beforeStartViewModel,
        SynchronizationMainStatusViewModel mainStatusViewModel, SynchronizationStatisticsViewModel statisticsViewModel)
    {
        BeforeStartViewModel = beforeStartViewModel;
        MainStatusViewModel = mainStatusViewModel;
        StatisticsViewModel = statisticsViewModel;

        this.WhenActivated(disposables =>
        {
            BeforeStartViewModel.Activator.Activate().DisposeWith(disposables);
            MainStatusViewModel.Activator.Activate().DisposeWith(disposables);
            StatisticsViewModel.Activator.Activate().DisposeWith(disposables);

            this.WhenAnyValue(x => x.MainStatusViewModel.HasSynchronizationStarted)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.HasSynchronizationStarted)
                .DisposeWith(disposables);
        });
    }

    public SynchronizationBeforeStartViewModel BeforeStartViewModel { get; } = null!;

    public SynchronizationMainStatusViewModel MainStatusViewModel { get; } = null!;

    public SynchronizationStatisticsViewModel StatisticsViewModel { get; } = null!;

    public extern bool HasSynchronizationStarted { [ObservableAsProperty] get; }
}
