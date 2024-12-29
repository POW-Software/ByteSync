using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using DynamicData;
using ReactiveUI;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class ManageSynchronizationRulesViewModel : ActivatableViewModelBase
{
    private readonly ISynchronizationRuleRepository _synchronizationRuleRepository;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISynchronizationRuleSummaryViewModelFactory _synchronizationRuleSummaryViewModelFactory;
    private readonly IDialogService _dialogService;
    private readonly IFlyoutElementViewModelFactory _flyoutElementViewModelFactory;
    
    private ReadOnlyObservableCollection<SynchronizationRuleSummaryViewModel> _bindingData;


    public ManageSynchronizationRulesViewModel(){}
    
    public ManageSynchronizationRulesViewModel(ISynchronizationRuleRepository synchronizationRuleRepository, ISynchronizationService synchronizationService,
        ISynchronizationRuleSummaryViewModelFactory synchronizationRuleSummaryViewModelFactory, IDialogService dialogService,
        IFlyoutElementViewModelFactory flyoutElementViewModelFactory)
    {
        _synchronizationRuleRepository = synchronizationRuleRepository;
        _synchronizationService = synchronizationService;
        _synchronizationRuleSummaryViewModelFactory = synchronizationRuleSummaryViewModelFactory;
        _dialogService = dialogService;
        _flyoutElementViewModelFactory = flyoutElementViewModelFactory;

        var canAddOrClearSynchronizationRules = _synchronizationService.SynchronizationProcessData.SynchronizationStart
            .Select(ss => ss == null)
            .ObserveOn(RxApp.MainThreadScheduler);
        
        _synchronizationRuleRepository.ObservableCache
            .Connect() // make the source an observable change set
            .Transform(sr => _synchronizationRuleSummaryViewModelFactory.Create(sr))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _bindingData)
            .Subscribe();
        
        AddSynchronizationRuleCommand = ReactiveCommand.Create(AddSynchronizationRule, canAddOrClearSynchronizationRules);
        ClearSynchronizationRulesCommand = ReactiveCommand.Create(ClearSynchronizationRules, canAddOrClearSynchronizationRules);
    }
    
    public ReactiveCommand<Unit, Unit> AddSynchronizationRuleCommand { get; set; }

    public ReactiveCommand<Unit, Unit> ClearSynchronizationRulesCommand { get; set; }
    
    public ReadOnlyObservableCollection<SynchronizationRuleSummaryViewModel> SynchronizationRules => _bindingData;
    
    private void AddSynchronizationRule()
    {
        _dialogService.ShowFlyout(nameof(Resources.Shell_CreateSynchronizationRule), false,
            _flyoutElementViewModelFactory.BuildSynchronizationRuleGlobalViewModel());
    }

    private void ClearSynchronizationRules()
    {
        _synchronizationRuleRepository.Clear();
    }
}