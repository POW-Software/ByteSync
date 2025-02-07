using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions;

public class AtomicActionEditViewModel : BaseAtomicEditViewModel
{
    // private readonly IActionEditionEventsHub _actionEditionEventsHub;

    private ObservableAsPropertyHelper<bool> _isSourceVisible;
    private ObservableAsPropertyHelper<bool> _isDestinationVisible;
    private ObservableAsPropertyHelper<bool> _isDestinationToVisible;
    private ObservableAsPropertyHelper<bool> _isDestinationOnVisible;
    private readonly IDataPartIndexer _dataPartIndexer;
    
    


    public AtomicActionEditViewModel()
    {
    }

    public AtomicActionEditViewModel(FileSystemTypes fileSystemTypes, bool showDeleteButton, List<ComparisonItem>? comparisonItems,
        IDataPartIndexer dataPartIndexer)
    {
        _dataPartIndexer = dataPartIndexer;
        
        FileSystemType = fileSystemTypes;
        ShowDeleteButton = showDeleteButton;
        ComparisonItems = comparisonItems;

        Actions = new ObservableCollection<ActionViewModel>();
        Sources = new ObservableCollection<DataPart>();
        Destinations = new ObservableCollection<DataPart>();
        
        RemoveCommand = ReactiveCommand.Create(Remove);
        
        var canSwapSides = this.WhenAnyValue(x => x.SelectedSource, x => x.SelectedDestination,
            (source, destination) => source != null && destination != null);
        SwapSidesCommand = ReactiveCommand.Create(SwapSides, canSwapSides);
            
        this.WhenAnyValue(
                x => x.SelectedAction,
                (selectedAction) => selectedAction != null 
                                    && !selectedAction.ActionOperatorType
                                        .In(ActionOperatorTypes.DoNothing, ActionOperatorTypes.Delete, ActionOperatorTypes.Create))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.IsSourceVisible, out _isSourceVisible);
        
        this.WhenAnyValue(
                x => x.SelectedAction,
                (selectedAction) => selectedAction != null && !selectedAction.ActionOperatorType.In(ActionOperatorTypes.DoNothing))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.IsDestinationVisible, out _isDestinationVisible);
        
        this.WhenAnyValue(
                x => x.SelectedAction,
                (selectedAction) => selectedAction != null && 
                                    selectedAction.ActionOperatorType.In(ActionOperatorTypes.SynchronizeContentAndDate, ActionOperatorTypes.SynchronizeContentOnly,
                                        ActionOperatorTypes.SynchronizeDate))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.IsDestinationToVisible, out _isDestinationToVisible);
        
        this.WhenAnyValue(
                x => x.SelectedAction,
                (selectedAction) => selectedAction != null && selectedAction.ActionOperatorType
                    .In(ActionOperatorTypes.Delete, ActionOperatorTypes.Create))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.IsDestinationOnVisible, out _isDestinationOnVisible);
        

        this.WhenAnyValue(x => x.SelectedSource)
            .Subscribe(_ =>
            {
                FillDestinations();
            });
        
        this.WhenAnyValue(x => x.SelectedAction,
                (selectedAction) => selectedAction != null && selectedAction.ActionOperatorType.In(ActionOperatorTypes.Delete))
            .Subscribe(_ =>
            {
                SelectedSource = null;
                SelectedDestination = null;
                FillDestinations();
            });

        FillActions();
        FillSources();
        FillDestinations();
    }

    public List<ComparisonItem>? ComparisonItems { get; set; }

    internal ObservableCollection<ActionViewModel> Actions { get; set; }

    internal ObservableCollection<DataPart> Sources { get; set; }

    internal ObservableCollection<DataPart> Destinations { get; set; }
    
    private FileSystemTypes FileSystemType { get; }

    [Reactive]
    internal ActionViewModel? SelectedAction { get; set; }

    [Reactive]
    internal DataPart? SelectedSource { get; set; }

    [Reactive]
    internal DataPart? SelectedDestination { get; set; }
        
    public bool IsSourceVisible => _isSourceVisible.Value;
    
    public bool IsDestinationVisible => _isDestinationVisible.Value;
    
    public bool IsDestinationToVisible => _isDestinationToVisible.Value;
    
    public bool IsDestinationOnVisible => _isDestinationOnVisible.Value;

    [Reactive]
    public bool ShowDeleteButton { get; set; }

    public ReactiveCommand<Unit, Unit> RemoveCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> SwapSidesCommand { get; set; }

    private void FillActions()
    {
        ActionViewModel actionViewModel;
        
        if (FileSystemType == FileSystemTypes.File)
        {
            actionViewModel = new ActionViewModel(ActionOperatorTypes.SynchronizeContentAndDate, Resources.AtomicActionEdit_SynchronizeContentAndDate);
            Actions.Add(actionViewModel);

            actionViewModel = new ActionViewModel(ActionOperatorTypes.SynchronizeContentOnly, Resources.AtomicActionEdit_SynchronizeContent);
            Actions.Add(actionViewModel);

            actionViewModel = new ActionViewModel(ActionOperatorTypes.SynchronizeDate, Resources.AtomicActionEdit_SynchronizeDate);
            Actions.Add(actionViewModel);
        }
        else if (FileSystemType == FileSystemTypes.Directory)
        {
            actionViewModel = new ActionViewModel(ActionOperatorTypes.Create, Resources.AtomicActionEdit_Create);
            Actions.Add(actionViewModel);
        }
        else
        {
            throw new ApplicationException("Unknown FileSystemType " + FileSystemType);
        }
        
        // Actions communes
        actionViewModel = new ActionViewModel(ActionOperatorTypes.Delete, Resources.AtomicActionEdit_Delete);
        Actions.Add(actionViewModel);

        actionViewModel = new ActionViewModel(ActionOperatorTypes.DoNothing, Resources.AtomicActionEdit_DoNothing);
        Actions.Add(actionViewModel);
    }

    private void FillSources()
    {
        Sources.Clear();
        Sources.AddAll(_dataPartIndexer.GetAllDataParts());
    }

    private void FillDestinations()
    {
        var selectedDestination = SelectedDestination;

        var destinations = _dataPartIndexer.GetAllDataParts().ToHashSet();
        if (SelectedSource != null)
        {
            destinations.Remove(SelectedSource);
        }

        Destinations.Clear();
        Destinations.AddAll(destinations);

        if (selectedDestination != null)
        {
            SelectedDestination = Destinations.FirstOrDefault(ad => ad.Equals(selectedDestination));
        }
        if (SelectedDestination == null && SelectedSource != null && Destinations.Count == 1)
        {
            SelectedDestination = Destinations.First();
        }

        if (SelectedDestination == null && ComparisonItems != null && 
            SelectedAction != null && SelectedAction.ActionOperatorType.In(ActionOperatorTypes.Create, ActionOperatorTypes.Delete))
        {
            // We look if only one destination can be determined
            HashSet<DataPart> possibleDataParts = new HashSet<DataPart>();
            foreach (var comparisonItem in ComparisonItems)
            {
                foreach (var dataPart in Destinations)
                {
                    var contentIdentities = comparisonItem.GetContentIdentities(dataPart.GetApplicableInventoryPart());
                        
                    if (SelectedAction is { ActionOperatorType: ActionOperatorTypes.Create } 
                        && contentIdentities.Count == 0)
                    {
                        possibleDataParts.Add(dataPart);
                    }
                        
                    if (SelectedAction is { ActionOperatorType: ActionOperatorTypes.Delete } 
                        && contentIdentities.Count != 0)
                    {
                        possibleDataParts.Add(dataPart);
                    }
                }
            }

            if (possibleDataParts.Count == 1)
            {
                SelectedDestination = possibleDataParts.Single();
            }
        }
    }

    internal AtomicAction? ExportSynchronizationAction()
    {
        if (SelectedAction == null)
        {
            return null;
        }

        if (SelectedAction.ActionOperatorType.In(ActionOperatorTypes.Create, ActionOperatorTypes.Delete) 
            && SelectedDestination == null)
        {
            return null;
        }
        
        if (SelectedAction.ActionOperatorType
                .In(ActionOperatorTypes.SynchronizeDate, ActionOperatorTypes.SynchronizeContentOnly, ActionOperatorTypes.SynchronizeContentAndDate) 
            && (SelectedSource == null || SelectedDestination == null))
        {
            return null;
        }
        
        var id = $"AAID_{Guid.NewGuid()}";
        var atomicAction = new AtomicAction();
        atomicAction.AtomicActionId = id;

        atomicAction.Operator = SelectedAction.ActionOperatorType;
        atomicAction.Source = SelectedSource;
        atomicAction.Destination = SelectedDestination;

        return atomicAction;
    }

    internal void SetSynchronizationAction(AtomicAction atomicAction)
    {
        SelectedAction = Actions.FirstOrDefault(a => Equals(a.ActionOperatorType, atomicAction.Operator));

        SelectedSource = atomicAction.Source;
        SelectedDestination = atomicAction.Destination;
    }


    private void Remove()
    {
        RaiseRemoveRequested();
    }
    
    private void SwapSides()
    {
        var source = SelectedSource;
        var destination = SelectedDestination;

        SelectedSource = destination;
        SelectedDestination = source;
    }
}