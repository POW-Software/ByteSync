using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Services.Comparisons.DescriptionBuilders;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class SynchronizationActionViewModel : ViewModelBase, IDisposable
{
    private readonly ISynchronizationService _synchronizationService = null!;
    private readonly ILocalizationService _localizationService = null!;
    private readonly ITargetedActionsService _targetedActionsService = null!;
    
    private readonly CompositeDisposable _compositeDisposable = null!;

    public SynchronizationActionViewModel()
    {
#if DEBUG
        Letter = "M";
#endif
    }

    public SynchronizationActionViewModel(AtomicAction atomicAction, ComparisonItemViewModel comparisonItemViewModel, 
        ILocalizationService localizationService, ISynchronizationService synchronizationService, 
        ITargetedActionsService targetedActionsService)
        : this()
    {
        _localizationService = localizationService;
        _synchronizationService = synchronizationService;
        _targetedActionsService = targetedActionsService;
        
        _compositeDisposable = new CompositeDisposable();
        
        AtomicAction = atomicAction;
        ComparisonItemViewModel = comparisonItemViewModel;

        IsFromSynchronizationRule = atomicAction.IsFromSynchronizationRule;

        RemoveCommand = ReactiveCommand.Create(Remove);
        EditCommand = ReactiveCommand.Create(Edit);

        _synchronizationService.SynchronizationProcessData.SynchronizationStart
            .Select(ss => ss == null && !atomicAction.IsFromSynchronizationRule)
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.IsEditableOrRemovable)
            .DisposeWith(_compositeDisposable);
        
        _localizationService.CurrentCultureObservable.ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => SetTexts())
            .DisposeWith(_compositeDisposable);
        
        SetTexts();
    }

    public AtomicAction AtomicAction { get; } = null!;

    public ComparisonItemViewModel ComparisonItemViewModel { get; } = null!;

    public ReactiveCommand<Unit, Unit> RemoveCommand { get; set; } = null!;
    public ReactiveCommand<Unit, Unit> EditCommand { get; set; } = null!;

    [Reactive]
    public string Actions { get; set; } = null!;

    [Reactive]
    public string Letter { get; set; }
    
    public extern bool IsEditableOrRemovable { [ObservableAsProperty] get; }

    [Reactive]
    public bool IsFromSynchronizationRule { get; set; }

    public bool IsTargeted
    {
        get
        {
            return !IsFromSynchronizationRule;
        }
    }
    
    private void Remove()
    {
        _targetedActionsService.RemoveTargetedAction(ComparisonItemViewModel, this);
    }

    private void Edit()
    {

    }

    public void OnLocaleChanged()
    {
        SetTexts();
    }

    private void SetTexts()
    {
        var synchronizationActionDescriptionBuilder = new AtomicActionDescriptionBuilder(_localizationService);
        Actions = synchronizationActionDescriptionBuilder.GetDescription(AtomicAction);

        if (IsFromSynchronizationRule)
        {
            Letter = _localizationService[nameof(Resources.SynchronizationActionView_Letter_Rule)];
        }
        else
        {
            Letter = _localizationService[nameof(Resources.SynchronizationActionView_Letter_Targeted)];
        }
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }
}