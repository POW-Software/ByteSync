using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using DynamicData;
using ReactiveUI;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class ManageSynchronizationRulesViewModel : ActivableViewModelBase
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
        
        // Observable.FromEventPattern<GenericEventArgs<SynchronizationRuleSummaryViewModel>>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SynchronizationRuleRemoved))
        //     .ObserveOn(RxApp.MainThreadScheduler)
        //     .Subscribe(evt => OnAutomaticActionRemoved(evt.EventArgs.Value))
        //     .DisposeWith(disposables);
        
        // _synchronizationService.SynchronizationProcessData.SynchronizationStart
        //     .Where(ss => ss != null)
        //     .Subscribe(ss => OnSynchronizationStarted(ss!))
        //     .DisposeWith(disposables);
        //
        // var canAddOrClearSynchronizationRules2 = this
        //     .WhenAnyValue(x => x.HasSynchronizationStarted,
        //         (isSyncStarted) => !isSyncStarted)
        //     .ObserveOn(RxApp.MainThreadScheduler);

        // var canAddOrClearSynchronizationRules = _synchronizationService.SynchronizationProcessData.SynchronizationStart
        //     .CombineLatest(_synchronizationService.SynchronizationProcessData.SynchronizationEnd)
        //     .Select(se => se.First != null && se.Second == null);

        var canAddOrClearSynchronizationRules = _synchronizationService.SynchronizationProcessData.SynchronizationStart
            .Select(ss => ss == null)
            .ObserveOn(RxApp.MainThreadScheduler);
        
        _synchronizationRuleRepository.ObservableCache
            .Connect() // make the source an observable change set
            .Transform(sr => _synchronizationRuleSummaryViewModelFactory.Create(sr))
            // .Sort(SortExpressionComparer<ComparisonItemViewModel>.Ascending(c => c.PathIdentity.LinkingKeyValue))
            // .Page(pager)
            .ObserveOn(RxApp.MainThreadScheduler)
            // .Do(changes => PageParameters.Update(changes.Response))
            // Make sure this line^^ is only right before the Bind()
            // This may be important to avoid threading issues if
            // 'mySource' is updated on a different thread.
            .Bind(out _bindingData)
            // .DisposeMany()
            .Subscribe();
        
        AddSynchronizationRuleCommand = ReactiveCommand.Create(AddSynchronizationRule, canAddOrClearSynchronizationRules);
        // ImportRulesFromProfileCommand = ReactiveCommand.CreateFromTask(ImportRulesFromProfile, canAddOrClearSynchronizationRules);
        ClearSynchronizationRulesCommand = ReactiveCommand.Create(ClearSynchronizationRules, canAddOrClearSynchronizationRules);
    }
    
    public ReactiveCommand<Unit, Unit> AddSynchronizationRuleCommand { get; set; }
    
    // public ReactiveCommand<Unit, Unit> ImportRulesFromProfileCommand { get; set; }

    public ReactiveCommand<Unit, Unit> ClearSynchronizationRulesCommand { get; set; }
    
    public ReadOnlyObservableCollection<SynchronizationRuleSummaryViewModel> SynchronizationRules => _bindingData;
    
    private void AddSynchronizationRule()
    {
        // _navigationEventsHub.RaiseSynchronizationRuleCreationRequested();
        
        _dialogService.ShowFlyout(nameof(Resources.Shell_CreateSynchronizationRule), false,
            _flyoutElementViewModelFactory.BuildSynchronizationRuleGlobalViewModel());
    }
    
    // todo https://dev.azure.com/PowSoftware/ByteSync/_workitems/edit/21
    /*
    private async Task ImportRulesFromProfile()
    {
        var importRulesFromProfileViewModel = new ImportRulesFromProfileViewModel();
        
        MessageBoxViewModel messageBoxViewModel = _dialogService.CreateMessageBoxViewModel(nameof(Resources.Shell_ImportRulesFromProfile));
        messageBoxViewModel.ShowOK = true;
        messageBoxViewModel.ShowCancel = true;
        messageBoxViewModel.MessageContent = importRulesFromProfileViewModel;

        importRulesFromProfileViewModel
            .WhenAnyValue(i => i.SynchronizationRuleViewModels)
            .Select(x => x != null)
            .Subscribe(messageBoxViewModel.CanExecuteOK);

        var result = await _dialogService.ShowMessageBoxAsync(messageBoxViewModel);
        if (result == MessageBoxResult.OK)
        {
            _synchronizationRulesService.SynchronizationRules!.AddAll(importRulesFromProfileViewModel.SynchronizationRuleViewModels!);
            
            await _comparisonItemsService.ApplySynchronizationRules();
        }
    }
    */

    private void ClearSynchronizationRules()
    {
        _synchronizationRuleRepository.Clear();
    }
    
    // Commented out on 2 jun 2024
    /*
    private void OnAutomaticActionRemoved(SynchronizationRuleSummaryViewModel synchronizationRuleSummaryViewModel)
    {
        SynchronizationRules!.Remove(synchronizationRuleSummaryViewModel);
        
        // todo 040423 Gérer le remove
        // SynchronizationRuleMatcher synchronizationRuleMatcher = new SynchronizationRuleMatcher(_sessionDataHolder);
        // synchronizationRuleMatcher.RemoveDeleted(_sessionDataHolder.ComparisonItems, synchronizationRuleSummaryViewModel);
        
        _synchronizationActionsService.UpdateSharedSynchronizationActions();
    }
    
    private void OnSynchronizationStarted(Synchronization synchronizationStart)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            HasSynchronizationStarted = true;
            
            foreach (var synchronizationRuleSummaryViewModel in SynchronizationRules!)
            {
                synchronizationRuleSummaryViewModel.OnSynchronizationStarted();
            }

            // todo 04/04/23 EMERGENCY, we can't leave the Fill like that!
            // if (!_sessionService.IsCurrentInstanceId(synchronizationStart.StartedBy))
            // {
            //     
            //     //  SynchronizationDataHelper.FillSynchronizationActions(synchronizationStartedEventArgs.SharedAtomicActions);
            // }

            await SynchronizationDataHelper.InitializeSynchronizationData();

            // todo 04/04/23
            // 22/06/2022: ici, ne devrait-on pas passer à _sessionDataHolder.ComparisonItems ou ComparisonItems.SourceCollection ?
            // foreach (ComparisonItemViewModel comparisonItemViewModel in ComparisonItems)
            // {
            //     foreach (var synchronizationActionViewModel in comparisonItemViewModel.SynchronizationActions)
            //     {
            //         synchronizationActionViewModel.OnSynchronizationStarted();
            //     }
            // }
        });
    }
    */
}