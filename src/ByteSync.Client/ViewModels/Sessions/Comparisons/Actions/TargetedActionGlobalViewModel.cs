using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions;

public class TargetedActionGlobalViewModel : FlyoutElementViewModel
{
    readonly ObservableAsPropertyHelper<bool> _canEditAction;

    private readonly ILocalizationService _localizationService;
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly IComparisonItemActionsManager _comparisonItemActionsManager;
    private readonly IAtomicActionConsistencyChecker _atomicActionConsistencyChecker;
    private readonly IAtomicActionRepository _atomicActionRepository;
    private readonly IActionEditViewModelFactory _actionEditViewModelFactory;

    public const string SYNCHRONIZATION_ACTION_PARAMETER = "SynchronizationAction";

    public TargetedActionGlobalViewModel() 
    {
#if DEBUG
        // ShowWarning = true;
#endif
    }
    
    // public TargetedActionGlobalViewModel(ComparisonResultViewModel requester, 
    //     List<ComparisonItemViewModel> comparisonItemViewModels) 
    //     : this (requester, comparisonItemViewModels, null, null, null, null, null)
    // {
    // #if DEBUG
    //     // ShowWarning = true;
    // #endif
    // }

    public TargetedActionGlobalViewModel(List<ComparisonItem> comparisonItems, 
        INavigationEventsHub navigationEventsHub, ILocalizationService localizationService,
        IComparisonItemActionsManager comparisonItemActionsManager, IAtomicActionConsistencyChecker atomicActionConsistencyChecker,
        IAtomicActionRepository atomicActionRepository, IActionEditViewModelFactory actionEditViewModelFactory)
    {
        ComparisonItems = comparisonItems;

        FileSystemType = ComparisonItems.Select(civm => civm.FileSystemType).ToHashSet().Single();
        
        _navigationEventsHub = navigationEventsHub;
        _localizationService = localizationService;
        _comparisonItemActionsManager = comparisonItemActionsManager;
        _atomicActionConsistencyChecker = atomicActionConsistencyChecker;
        _atomicActionRepository = atomicActionRepository;
        _actionEditViewModelFactory = actionEditViewModelFactory;

        Actions = new ObservableCollection<AtomicActionEditViewModel>();
            
        // ComparisonResult = _sessionDataHolder.ComparisonResult;

        var canSave = this
            .WhenAnyValue(
                x => x.ShowWarning, x=> x.ShowSaveValidItemsCommand,
                (showWarning, showSaveValidItemsCommand) => !showWarning || !showSaveValidItemsCommand)
            .ObserveOn(RxApp.MainThreadScheduler);
            
        canSave
            .ToProperty(this, x => x.CanEditAction, out _canEditAction);

        this
            .WhenAnyValue(x => x.AreMissingFields, x => x.IsInconsistentWithValidItems, x => x.IsInconsistentWithNoValidItems,
                (areMissingFields, isInconsistentWithValidItems, isInconsistentWithNoValidItems) =>
                    areMissingFields || isInconsistentWithValidItems != null || isInconsistentWithNoValidItems)
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.ShowWarning);

        AddActionCommand = ReactiveCommand.Create(AddAction);
        SaveCommand = ReactiveCommand.Create(Save, canSave);
        ResetCommand = ReactiveCommand.Create(Reset);
        CancelCommand = ReactiveCommand.Create(Cancel);

        SaveValidItemsCommand = ReactiveCommand.Create(SaveValidItems);

        ResetWarning();

        Reset();

        // _actionEditionEventsHub.GetEvent<SynchronizationActionEditionChanged>().Subscribe(OnSynchronizationActionEditionChanged);
        
        this.WhenActivated(disposables =>
        {
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleChanged())
                .DisposeWith(disposables);
            
            // Observable.FromEventPattern<GenericEventArgs<AtomicActionEditViewModel>>(_actionEditionEventsHub, 
            //         nameof(_actionEditionEventsHub.AtomicActionInputChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnAtomicInputChanged())
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<GenericEventArgs<AtomicConditionEditViewModel>>(_actionEditionEventsHub, 
            //         nameof(_actionEditionEventsHub.AtomicConditionInputChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnAtomicInputChanged())
            //     .DisposeWith(disposables);
        });
    }

    public List<ComparisonItem> ComparisonItems { get; private set; }

    public ReactiveCommand<Unit, Unit> AddActionCommand { get; set; }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; set; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; set; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

    public ReactiveCommand<Unit, Unit> SaveValidItemsCommand { get; set; }

    // private ComparisonResult ComparisonResult { get; set; }

    internal AtomicAction? BaseSynchronizationAction { get; set; }
        
    // [Reactive] // faut-il Reactive ici ? => Ben non, pas de setter
    internal ObservableCollection<AtomicActionEditViewModel> Actions { get; }
    
    private FileSystemTypes FileSystemType { get; }

    // Utilisation de Fody
    public extern bool ShowWarning { [ObservableAsProperty]get; }

    [Reactive]
    public bool ShowSaveValidItemsCommand { get; set; }

    public bool CanEditAction => _canEditAction.Value;

    [Reactive]
    public string SaveWarning { get; set; }
    
    [Reactive]
    public bool AreMissingFields { get; set; }
    
    [Reactive]
    public Tuple<int, int>? IsInconsistentWithValidItems { get; set; }
    
    [Reactive]
    public bool IsInconsistentWithNoValidItems { get; set; }

    private void AddAction()
    {
        var atomicActionEditViewModel = 
            _actionEditViewModelFactory.BuildAtomicActionEditViewModel(FileSystemType, false, ComparisonItems);
        
        atomicActionEditViewModel.PropertyChanged += AtomicActionEditViewModelOnPropertyChanged;
        
        // AtomicActionEditViewModel atomicActionEditViewModel =
        //     new AtomicActionEditViewModel(FileSystemType, false, ComparisonItemViewModels, _actionEditionEventsHub);
            
        Actions.Add(atomicActionEditViewModel);
    }

    private void AtomicActionEditViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ResetWarning();
    }


    private void Save()
    {
        var atomicAction = Export();

        if (atomicAction == null)
        {
            ShowMissingFieldsWarning();
        }
        else
        {
            var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, ComparisonItems);
            
            
            // var atomicActionConsistencyChecker = new AtomicActionConsistencyChecker(atomicAction, ComparisonItemViewModels);
            if (result.IsOK)
            {
                ResetWarning();
                
                _comparisonItemActionsManager.AddTargetedAction(atomicAction, ComparisonItems);

                _navigationEventsHub.RaiseCloseFlyoutRequested();
                // _synchronizationActionsService.UpdateSharedSynchronizationActions();
            }
            else
            {
                ShowConsistencyWarning(result);
            }
        }
    }

    private void SaveValidItems()
    {
        var atomicAction = Export();
        if (atomicAction == null)
        {
            ShowMissingFieldsWarning();
        }
        else
        {
            var result = _atomicActionConsistencyChecker.CheckCanAdd(atomicAction, ComparisonItems);
            // var atomicActionConsistencyChecker = new AtomicActionConsistencyChecker(atomicAction, ComparisonItemViewModels);
            
            ResetWarning();

            _comparisonItemActionsManager.AddTargetedAction(atomicAction, result.ValidComparisons);

            _navigationEventsHub.RaiseCloseFlyoutRequested();
            // _synchronizationActionsService.UpdateSharedSynchronizationActions();
        }
    }

    private bool CanSave()
    {
        return !ShowWarning || !ShowSaveValidItemsCommand;
    }

    private AtomicAction? Export()
    {
        var automaticActionsActionEditViewModel = Actions.Single(); // (AutomaticActionsActionEditViewModel)region.Views.Single();
        var atomicAction = automaticActionsActionEditViewModel.ExportSynchronizationAction();

        if (atomicAction != null)
        {
            if (BaseSynchronizationAction != null)
            {
                atomicAction.AtomicActionId = BaseSynchronizationAction.AtomicActionId;
            }
        }


        return atomicAction;
    }

    private void Reset()
    {
        if (BaseSynchronizationAction == null)
        {
            ResetToCreation();
        }
        else
        {
            ResetToEdition();
        }

        ResetWarning();
    }

    private void ResetToCreation()
    {
        ClearActions();

        AddAction();
    }

    private void ResetToEdition()
    {
        ClearActions();

        var atomicActionEditViewModel = _actionEditViewModelFactory.BuildAtomicActionEditViewModel(FileSystemType, false,
            ComparisonItems, BaseSynchronizationAction);
            
            // new AtomicActionEditViewModel(FileSystemType, false, 
            // ComparisonItemViewModels, _actionEditionEventsHub);
            
        atomicActionEditViewModel.PropertyChanged += AtomicActionEditViewModelOnPropertyChanged;
            
        atomicActionEditViewModel.SetSynchronizationAction(BaseSynchronizationAction!);
        
        Actions.Add(atomicActionEditViewModel);
    }
    
    private void ClearActions()
    {
        foreach (var atomicActionEditViewModel in Actions)
        {
            atomicActionEditViewModel.PropertyChanged -= AtomicActionEditViewModelOnPropertyChanged;
        }
        
        Actions.Clear();
    }

    private void Cancel()
    {
        RaiseCloseFlyoutRequested();

        Reset();
    }
    
    private void OnLocaleChanged()
    {
        DoShowWarning();
    }
    
    private void ShowMissingFieldsWarning()
    {
        AreMissingFields = true;
        IsInconsistentWithValidItems = null;
        IsInconsistentWithNoValidItems = false;

        DoShowWarning();
    }
    
    private void ShowConsistencyWarning(AtomicActionConsistencyCheckCanAddResult checkCanAddResult)
    {
        ShowSaveValidItemsCommand = checkCanAddResult.ValidComparisons.Count > 0;

        if (ShowSaveValidItemsCommand)
        {
            IsInconsistentWithValidItems = new Tuple<int, int>(checkCanAddResult.ValidComparisons.Count, 
                checkCanAddResult.NonValidComparisons.Count);
            IsInconsistentWithNoValidItems = false;
        }
        else
        {
            IsInconsistentWithNoValidItems = true;
            IsInconsistentWithValidItems = null;
        }
        
        AreMissingFields = false;

        DoShowWarning();
    }

    private void DoShowWarning()
    {
        var saveWarning = "";
        
        if (AreMissingFields)
        {
            saveWarning = Resources.TargetedActionEditionGlobal_MissingFields;
        }
        else if (IsInconsistentWithValidItems != null)
        {
            saveWarning = Resources.TargetedActionEditionGlobal_SaveWarning;
            saveWarning = saveWarning.Replace("{{VALID_ITEMS}}", IsInconsistentWithValidItems.Item1.ToString());
            saveWarning = saveWarning.Replace("{{NON_VALID_ITEMS}}", IsInconsistentWithValidItems.Item2.ToString());
        }
        else if (IsInconsistentWithNoValidItems)
        {
            saveWarning = Resources.TargetedActionEditionGlobal_SaveWarningLocked;
        }

        SaveWarning = saveWarning;
    }

    private void OnAtomicInputChanged()
    {
        ResetWarning();
    }

    private void ResetWarning()
    {
        AreMissingFields = false;
        IsInconsistentWithValidItems = null;
        IsInconsistentWithNoValidItems = false;
    }
}