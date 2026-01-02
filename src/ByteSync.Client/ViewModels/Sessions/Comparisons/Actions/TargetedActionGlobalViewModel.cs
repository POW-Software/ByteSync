using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions;

public class TargetedActionGlobalViewModel : FlyoutElementViewModel
{
    readonly ObservableAsPropertyHelper<bool> _canEditAction = null!;
    
    private readonly ILocalizationService _localizationService = null!;
    private readonly IDialogService _dialogService = null!;
    private readonly ITargetedActionsService _targetedActionsService = null!;
    private readonly IAtomicActionConsistencyChecker _atomicActionConsistencyChecker = null!;
    private readonly IActionEditViewModelFactory _actionEditViewModelFactory = null!;
    private readonly IAtomicActionValidationFailureReasonService _failureReasonLocalizer = null!;
    private readonly ILogger<TargetedActionGlobalViewModel> _logger = null!;
    
    public TargetedActionGlobalViewModel()
    {
    }
    
    public TargetedActionGlobalViewModel(List<ComparisonItem> comparisonItems,
        IDialogService dialogService, ILocalizationService localizationService,
        ITargetedActionsService targetedActionsService, IAtomicActionConsistencyChecker atomicActionConsistencyChecker,
        IActionEditViewModelFactory actionEditViewModelFactory,
        ILogger<TargetedActionGlobalViewModel> logger,
        IAtomicActionValidationFailureReasonService failureReasonLocalizer)
    {
        ComparisonItems = comparisonItems;
        
        FileSystemType = ComparisonItems.Select(civm => civm.FileSystemType).ToHashSet().Single();
        
        _dialogService = dialogService;
        _localizationService = localizationService;
        _targetedActionsService = targetedActionsService;
        _atomicActionConsistencyChecker = atomicActionConsistencyChecker;
        _actionEditViewModelFactory = actionEditViewModelFactory;
        _failureReasonLocalizer = failureReasonLocalizer;
        _logger = logger;
        
        // Initialize localized messages
        ActionIssuesHeaderMessage = _localizationService[nameof(Resources.TargetedActionEditionGlobal_ActionIssues)];
        AffectedItemsTooltipHeader = _localizationService[nameof(Resources.TargetedActionEditionGlobal_AffectedItemsTooltip)];
        
        Actions = new ObservableCollection<AtomicActionEditViewModel>();
        
        var canSave = this
            .WhenAnyValue(
                x => x.ShowWarning, x => x.ShowSaveValidItemsCommand,
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
    
    public List<ComparisonItem> ComparisonItems { get; private set; } = null!;
    
    public ReactiveCommand<Unit, Unit> AddActionCommand { get; set; } = null!;
    
    public ReactiveCommand<Unit, Unit> SaveCommand { get; set; } = null!;
    
    public ReactiveCommand<Unit, Unit> ResetCommand { get; set; } = null!;
    
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; } = null!;
    
    public ReactiveCommand<Unit, Unit> SaveValidItemsCommand { get; set; } = null!;
    
    internal AtomicAction? BaseAtomicAction { get; set; }
    
    internal ObservableCollection<AtomicActionEditViewModel> Actions { get; } = null!;
    
    private FileSystemTypes FileSystemType { get; }
    
    public extern bool ShowWarning { [ObservableAsProperty] get; }
    
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
    
    public ObservableCollection<ValidationFailureSummary> FailureSummaries { get; set; } = new();
    
    [Reactive]
    public string ActionIssuesHeaderMessage { get; set; } = string.Empty;
    
    [Reactive]
    public string AffectedItemsTooltipHeader { get; set; } = string.Empty;
    
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
                LogConsistencySuccess(atomicAction, ComparisonItems.Count);
                
                _dialogService.CloseFlyout();
            }
            else
            {
                ShowConsistencyWarning(atomicAction, result);
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
            LogConsistencySuccess(atomicAction, result.GetValidComparisonItems().Count);
            
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
        
        var atomicActionEditViewModel =
            _actionEditViewModelFactory.BuildAtomicActionEditViewModel(FileSystemType, false, BaseAtomicAction, ComparisonItems);
        
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
        // Update localized messages when locale changes
        ActionIssuesHeaderMessage = _localizationService[nameof(Resources.TargetedActionEditionGlobal_ActionIssues)];
        AffectedItemsTooltipHeader = _localizationService[nameof(Resources.TargetedActionEditionGlobal_AffectedItemsTooltip)];
        
        // Update localized messages in FailureSummaries if any
        foreach (var summary in FailureSummaries)
        {
            summary.LocalizedMessage = _failureReasonLocalizer.GetLocalizedMessage(summary.Reason);
        }
        
        DoShowWarning();
    }
    
    private void ShowMissingFieldsWarning()
    {
        AreMissingFields = true;
        IsInconsistentWithValidItems = null;
        IsInconsistentWithNoValidItems = false;
        FailureSummaries.Clear(); // Clear previous validation failure summaries
        
        DoShowWarning();
    }
    
    private void ShowConsistencyWarning(AtomicAction atomicAction, AtomicActionConsistencyCheckCanAddResult checkCanAddResult)
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
        
        // Generate failure summaries with detailed information
        FailureSummaries.Clear();
        var summaries = checkCanAddResult.FailedValidations
            .GroupBy(f => f.FailureReason!.Value)
            .Select(g => new ValidationFailureSummary
            {
                Reason = g.Key,
                Count = g.Count(),
                LocalizedMessage = _failureReasonLocalizer.GetLocalizedMessage(g.Key),
                AffectedItems = g.Select(f => f.ComparisonItem).ToList()
            })
            .OrderByDescending(s => s.Count)
            .ToList(); // Most frequent failures first
        
        foreach (var summary in summaries)
        {
            FailureSummaries.Add(summary);
        }
        
        LogConsistencyFailure(atomicAction, checkCanAddResult, summaries);
        
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
        FailureSummaries.Clear();
    }
    
    private void LogConsistencyFailure(AtomicAction atomicAction, AtomicActionConsistencyCheckCanAddResult result,
        IEnumerable<ValidationFailureSummary> summaries)
    {
        var validItemsCount = result.GetValidComparisonItems().Count;
        var invalidItemsCount = result.GetInvalidComparisonItems().Count;
        var failureDetails = BuildFailureDetails(summaries);
        
        _logger.LogWarning(
            "Targeted action validation failed. Operator={Operator} Source={Source} Destination={Destination} FileSystemType={FileSystemType} TotalItems={TotalItems} ValidItems={ValidItems} InvalidItems={InvalidItems} Failures={Failures}",
            atomicAction.Operator,
            atomicAction.SourceName ?? string.Empty,
            atomicAction.DestinationName ?? string.Empty,
            FileSystemType,
            result.ComparisonItems.Count,
            validItemsCount,
            invalidItemsCount,
            failureDetails);
    }
    
    private void LogConsistencySuccess(AtomicAction atomicAction, int itemsCount)
    {
        _logger.LogInformation(
            "Targeted action created. Operator={Operator} Source={Source} Destination={Destination} FileSystemType={FileSystemType} ItemsCount={ItemsCount}",
            atomicAction.Operator,
            atomicAction.SourceName ?? string.Empty,
            atomicAction.DestinationName ?? string.Empty,
            FileSystemType,
            itemsCount);
    }
    
    private static string BuildFailureDetails(IEnumerable<ValidationFailureSummary> summaries)
    {
        const int maxItemsPerReason = 3;
        
        var details = summaries
            .Select(summary =>
            {
                var items = summary.AffectedItems
                    .Select(item => item.PathIdentity.LinkingKeyValue)
                    .Take(maxItemsPerReason)
                    .ToList();
                
                var suffix = summary.AffectedItems.Count > maxItemsPerReason ? ", ..." : string.Empty;
                
                return $"{summary.Reason}={summary.Count} [{string.Join(", ", items)}{suffix}]";
            })
            .ToList();
        
        return details.Count == 0 ? "none" : string.Join("; ", details);
    }
}