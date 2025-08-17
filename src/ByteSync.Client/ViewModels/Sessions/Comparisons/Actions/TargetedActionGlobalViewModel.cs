using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions;

public class TargetedActionGlobalViewModel : FlyoutElementViewModel
{
    readonly ObservableAsPropertyHelper<bool> _canEditAction;

    private readonly ILocalizationService _localizationService;
    private readonly IDialogService _dialogService;
    private readonly ITargetedActionsService _targetedActionsService;
    private readonly IAtomicActionConsistencyChecker _atomicActionConsistencyChecker;
    private readonly IActionEditViewModelFactory _actionEditViewModelFactory;

    public TargetedActionGlobalViewModel() 
    {

    }

    public TargetedActionGlobalViewModel(List<ComparisonItem> comparisonItems, 
        IDialogService dialogService, ILocalizationService localizationService,
        ITargetedActionsService targetedActionsService, IAtomicActionConsistencyChecker atomicActionConsistencyChecker,
        IActionEditViewModelFactory actionEditViewModelFactory)
    {
        ComparisonItems = comparisonItems;

        FileSystemType = ComparisonItems.Select(civm => civm.FileSystemType).ToHashSet().Single();
        
        _dialogService = dialogService;
        _localizationService = localizationService;
        _targetedActionsService = targetedActionsService;
        _atomicActionConsistencyChecker = atomicActionConsistencyChecker;
        _actionEditViewModelFactory = actionEditViewModelFactory;

        Actions = new ObservableCollection<AtomicActionEditViewModel>();

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
        
        this.WhenActivated(disposables =>
        {
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleChanged())
                .DisposeWith(disposables);
        });
    }

    public List<ComparisonItem> ComparisonItems { get; private set; }

    public ReactiveCommand<Unit, Unit> AddActionCommand { get; set; }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; set; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; set; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

    public ReactiveCommand<Unit, Unit> SaveValidItemsCommand { get; set; }

    internal AtomicAction? BaseAtomicAction { get; set; }
    internal ObservableCollection<AtomicActionEditViewModel> Actions { get; }
    
    private FileSystemTypes FileSystemType { get; }
    
    public extern bool ShowWarning { [ObservableAsProperty]get; }

    [Reactive]
    public bool ShowSaveValidItemsCommand { get; set; }

    public bool CanEditAction => _canEditAction.Value;

    [Reactive]
    public string SaveWarning { get; set; } = string.Empty;
    
    [Reactive]
    public bool AreMissingFields { get; set; }
    
    [Reactive]
    public Tuple<int, int>? IsInconsistentWithValidItems { get; set; }
    
    [Reactive]
    public bool IsInconsistentWithNoValidItems { get; set; }

    private void AddAction()
    {
        var atomicActionEditViewModel = 
            _actionEditViewModelFactory.BuildAtomicActionEditViewModel(FileSystemType, false, null, ComparisonItems);
        
        atomicActionEditViewModel.PropertyChanged += AtomicActionEditViewModelOnPropertyChanged;
            
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
            
            if (result.IsOK)
            {
                ResetWarning();
                
                _targetedActionsService.AddTargetedAction(atomicAction, ComparisonItems);

                _dialogService.CloseFlyout();
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
            
            ResetWarning();

            _targetedActionsService.AddTargetedAction(atomicAction, result.GetValidComparisonItems());

            _dialogService.CloseFlyout();
        }
    }

    private AtomicAction? Export()
    {
        var automaticActionsActionEditViewModel = Actions.Single(); // (AutomaticActionsActionEditViewModel)region.Views.Single();
        var atomicAction = automaticActionsActionEditViewModel.ExportSynchronizationAction();

        if (atomicAction != null)
        {
            if (BaseAtomicAction != null)
            {
                atomicAction.AtomicActionId = BaseAtomicAction.AtomicActionId;
            }
        }


        return atomicAction;
    }

    private void Reset()
    {
        if (BaseAtomicAction == null)
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

        var atomicActionEditViewModel = _actionEditViewModelFactory.BuildAtomicActionEditViewModel(FileSystemType, false, BaseAtomicAction, ComparisonItems);

        atomicActionEditViewModel.PropertyChanged += AtomicActionEditViewModelOnPropertyChanged;
            
        atomicActionEditViewModel.SetAtomicAction(BaseAtomicAction!);
        
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
        ShowSaveValidItemsCommand = checkCanAddResult.GetValidComparisonItems().Count > 0;

        if (ShowSaveValidItemsCommand)
        {
            IsInconsistentWithValidItems = new Tuple<int, int>(checkCanAddResult.GetValidComparisonItems().Count, 
                checkCanAddResult.GetInvalidComparisonItems().Count);
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