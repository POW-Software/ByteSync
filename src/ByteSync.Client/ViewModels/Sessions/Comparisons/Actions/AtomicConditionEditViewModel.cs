using System.Collections.ObjectModel;
using System.Reactive;
using ByteSync.Assets.Resources;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Converters;
using ByteSync.ViewModels.Sessions.Comparisons.Actions.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Actions;

public class AtomicConditionEditViewModel : BaseAtomicEditViewModel
{
    private readonly IDataPartIndexer _dataPartIndexer;

    public AtomicConditionEditViewModel()
    {
#if DEBUG
        IsDateVisible = true;
        IsSizeVisible = true;
#endif
    }

    public AtomicConditionEditViewModel(FileSystemTypes fileSystemType, IDataPartIndexer dataPartIndexer)
    {
        _dataPartIndexer = dataPartIndexer;
        
        FileSystemType = fileSystemType;

        SourceOrProperties = new ObservableCollection<SourceOrPropertyViewModel>();
        ComparisonProperties = new ObservableCollection<ComparisonPropertyViewModel>();
        ComparisonOperators = new ObservableCollection<ConditionOperatorViewModel>();
        ConditionDestinations = new ObservableCollection<DataPart>();
        SizeUnits = new ObservableCollection<SizeUnitViewModel>();
        NamePattern = string.Empty;

        SelectedDateTime = DateTime.Now;
        SelectedTime = SelectedDateTime.Value.TimeOfDay;
        
        RemoveCommand = ReactiveCommand.Create(Remove);

        var canSwapSides = this.WhenAnyValue(x => x.SelectedSourceOrProperty, x => x.SelectedDestination,
            (source, destination) => source is { IsDataPart: true } && destination is { IsVirtual: false });
        SwapSidesCommand = ReactiveCommand.Create(SwapSides, canSwapSides);

        FillSourceOrPropertiesData();

        FillConditionSourceTypes();

        FillAvailableOperators();

        FillAvailableDestinations();

        FillSizeUnits();

        this.WhenAnyValue(x => x.SelectedSourceOrProperty)
            .Subscribe(_ => 
            {
                FillAvailableOperators();
                FillAvailableDestinations();
                ShowHideControls();
            });
            
        this.WhenAnyValue(x => x.SelectedComparisonElement)
            .Subscribe(_ =>
            {
                FillAvailableOperators();
                FillAvailableDestinations();
                ShowHideControls();
            });
            
        this.WhenAnyValue(x => x.SelectedComparisonOperator)
            .Subscribe(_ =>
            {
                FillAvailableDestinations();
                ShowHideControls();
            });
            
        this.WhenAnyValue(x => x.SelectedDestination)
            .Subscribe(_ => ShowHideControls());
        
        SelectedSizeUnit = SizeUnits.Single(su => su.SizeUnit == Common.Business.Misc.SizeUnits.KB);
        
        ShowHideControls();
    }

    public FileSystemTypes FileSystemType { get; }

    internal ObservableCollection<SourceOrPropertyViewModel> SourceOrProperties { get; set; }

    internal ObservableCollection<ComparisonPropertyViewModel> ComparisonProperties { get; set; }

    internal ObservableCollection<ConditionOperatorViewModel> ComparisonOperators { get; set; }

    internal ObservableCollection<DataPart> ConditionDestinations { get; set; }
        
    internal ObservableCollection<TimeZoneInfo> TimeZones { get; set; }

    internal ObservableCollection<SizeUnitViewModel> SizeUnits { get; set; }

    [Reactive]
    internal SourceOrPropertyViewModel? SelectedSourceOrProperty { get; set; }

    [Reactive]
    internal ComparisonPropertyViewModel? SelectedComparisonElement { get; set; }

    [Reactive]
    internal ConditionOperatorViewModel? SelectedComparisonOperator { get; set; }

    [Reactive]
    internal DataPart? SelectedDestination { get; set; }

    [Reactive]
    public DateTimeOffset? SelectedDateTime { get; set; }
    
    [Reactive]
    public TimeSpan SelectedTime { get; set; }

    [Reactive]
    public int? SelectedSize { get; set; }

    [Reactive]
    internal SizeUnitViewModel SelectedSizeUnit { get; set; }

    [Reactive]
    public string? NamePattern { get; set; }

    [Reactive]
    public bool IsNameVisible { get; set; }

    [Reactive]
    public bool IsDateVisible { get; set; }

    [Reactive]
    public bool IsSizeVisible { get; set; }

    [Reactive]
    public bool IsSourceTypeComboBoxVisible { get; set; }

    [Reactive]
    public bool IsDotVisible { get; set; }

    [Reactive]
    public bool IsDestinationComboBoxVisible { get; set; }

    public ReactiveCommand<Unit, Unit> RemoveCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> SwapSidesCommand { get; set; }

    private void FillSourceOrPropertiesData()
    {
        SourceOrProperties.Clear();
        
        // Add sources (DataPart)
        SourceOrProperties.AddAll(_dataPartIndexer.GetAllDataParts().Select(dp => new SourceOrPropertyViewModel(dp)));
        
        // Add the Name property
        var nameProperty = new SourceOrPropertyViewModel(ComparisonProperty.Name, Resources.AtomicConditionEdit_Name);
        SourceOrProperties.Add(nameProperty);
    }

    private void FillConditionSourceTypes()
    {
        ComparisonPropertyViewModel comparisonPropertyView;
        
        if (FileSystemType == FileSystemTypes.File)
        {
            comparisonPropertyView = new ComparisonPropertyViewModel
            {
                ComparisonProperty = ComparisonProperty.Content,
                Description = Resources.AtomicConditionEdit_Content
            };
            ComparisonProperties.Add(comparisonPropertyView);

            comparisonPropertyView = new ComparisonPropertyViewModel
            {
                ComparisonProperty = ComparisonProperty.Date,
                Description = Resources.AtomicConditionEdit_LastWriteTime
            };
            ComparisonProperties.Add(comparisonPropertyView);

            comparisonPropertyView = new ComparisonPropertyViewModel
            {
                ComparisonProperty = ComparisonProperty.Size,
                Description = Resources.AtomicConditionEdit_Size
            };
            ComparisonProperties.Add(comparisonPropertyView);

            comparisonPropertyView = new ComparisonPropertyViewModel
            {
                ComparisonProperty = ComparisonProperty.Presence,
                Description = Resources.AtomicConditionEdit_Presence
            };
            ComparisonProperties.Add(comparisonPropertyView);   
        }
        else
        {
            comparisonPropertyView = new ComparisonPropertyViewModel
            {
                ComparisonProperty = ComparisonProperty.Presence,
                Description = Resources.AtomicConditionEdit_Presence
            };
            ComparisonProperties.Add(comparisonPropertyView);
        }
    }

    private void FillAvailableOperators()
    {
        var selectedOperator = SelectedComparisonOperator;

        ComparisonOperators.Clear();

        if (FileSystemType == FileSystemTypes.File)
        {
            if (SelectedSourceOrProperty?.IsDataPart == true && (SelectedComparisonElement == null ||
                SelectedComparisonElement.ComparisonProperty == ComparisonProperty.Content))
            {
                var conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.Equals);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.NotEquals);
                ComparisonOperators.Add(conditionOperatorView);
            }
            else if (SelectedSourceOrProperty?.IsDataPart == true && SelectedComparisonElement?.ComparisonProperty == ComparisonProperty.Date)
            {
                var conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.Equals);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.NotEquals);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.IsOlderThan);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.IsNewerThan);
                ComparisonOperators.Add(conditionOperatorView);
            }
            else if (SelectedSourceOrProperty?.IsDataPart == true && SelectedComparisonElement?.ComparisonProperty == ComparisonProperty.Size)
            {
                var conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.Equals);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.NotEquals);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.IsBiggerThan);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.IsSmallerThan);
                ComparisonOperators.Add(conditionOperatorView);
            }
            else if (SelectedSourceOrProperty?.IsDataPart == true && SelectedComparisonElement?.ComparisonProperty == ComparisonProperty.Name)
            {
                var conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.Equals);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.NotEquals);
                ComparisonOperators.Add(conditionOperatorView);
            }
            else if (SelectedSourceOrProperty?.IsDataPart == true && SelectedComparisonElement?.ComparisonProperty == ComparisonProperty.Presence)
            {
                var conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.ExistsOn);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.NotExistsOn);
                ComparisonOperators.Add(conditionOperatorView);
            }
            else if (SelectedSourceOrProperty?.IsNameProperty == true)
            {
                var conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.Equals);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.NotEquals);
                ComparisonOperators.Add(conditionOperatorView);
            }
        }
        else if (FileSystemType == FileSystemTypes.Directory)
        {
            if (SelectedSourceOrProperty?.IsDataPart == true && (SelectedComparisonElement == null ||
                SelectedComparisonElement.ComparisonProperty == ComparisonProperty.Presence))
            {
                var conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.ExistsOn);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.NotExistsOn);
                ComparisonOperators.Add(conditionOperatorView);
            }
            else if (SelectedSourceOrProperty?.IsDataPart == true && SelectedComparisonElement?.ComparisonProperty == ComparisonProperty.Name)
            {
                var conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.Equals);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.NotEquals);
                ComparisonOperators.Add(conditionOperatorView);
            }
            else if (SelectedSourceOrProperty?.IsNameProperty == true)
            {
                var conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.Equals);
                ComparisonOperators.Add(conditionOperatorView);

                conditionOperatorView = BuildConditionOperatorView(ConditionOperatorTypes.NotEquals);
                ComparisonOperators.Add(conditionOperatorView);
            }
        }
        else
        {
            throw new ApplicationException("Unknown FileSystemType " + FileSystemType);
        }

        if (selectedOperator != null)
        {
            SelectedComparisonOperator = ComparisonOperators.FirstOrDefault(ao => ao.Equals(selectedOperator));
        }
    }

    private void FillAvailableDestinations()
    {
        var selectedDestination = SelectedDestination;

        ConditionDestinations.Clear();

        bool addCustomDestination = false;
        bool selectCustomDestination = false;
        
        if (SelectedSourceOrProperty?.IsProperty == true)
        {
            addCustomDestination = true;
            selectCustomDestination = true;
        }
        else
        {
            ConditionDestinations.AddAll(_dataPartIndexer.GetAllDataParts());

            if (SelectedComparisonElement is { IsDateOrSize: true }
                && SelectedComparisonOperator != null
                && !SelectedComparisonOperator.ConditionOperator.In(ConditionOperatorTypes.ExistsOn, ConditionOperatorTypes.NotExistsOn))
            {
                addCustomDestination = true;
            }
        }
        
        if (addCustomDestination)
        {
            var conditionData = new DataPart(Resources.AtomicConditionEdit_Custom);
            ConditionDestinations.Add(conditionData);
            
            if (selectCustomDestination)
            {
                selectedDestination = conditionData;
            }
        }

        if (selectedDestination != null)
        {
            SelectedDestination = ConditionDestinations.FirstOrDefault(ad => ad.Equals(selectedDestination));
        }
    }

    private void FillSizeUnits()
    {
        var sizeUnitsList = new List<Common.Business.Misc.SizeUnits>();
        sizeUnitsList.Add(Common.Business.Misc.SizeUnits.Byte);
        sizeUnitsList.Add(Common.Business.Misc.SizeUnits.KB);
        sizeUnitsList.Add(Common.Business.Misc.SizeUnits.MB);
        sizeUnitsList.Add(Common.Business.Misc.SizeUnits.GB);
        sizeUnitsList.Add(Common.Business.Misc.SizeUnits.TB);

        var sizeUnitConverter = new SizeUnitConverter();
        foreach (var sizeUnit in sizeUnitsList)
        {
            var sizeUnitView = new SizeUnitViewModel(sizeUnit, sizeUnitConverter.GetPrintableSizeUnit(sizeUnit));
            SizeUnits.Add(sizeUnitView);
        }
    }

    private ConditionOperatorViewModel BuildConditionOperatorView(ConditionOperatorTypes conditionOperatorType)
    {
        string description;
        switch (conditionOperatorType)
        {
            case ConditionOperatorTypes.Equals:
                description = Resources.AtomicConditionEdit_Equals;
                break;
            case ConditionOperatorTypes.NotEquals:
                description = Resources.AtomicConditionEdit_NotEquals;
                break;
            case ConditionOperatorTypes.ExistsOn:
                description = Resources.AtomicConditionEdit_ExistsOn;
                break;
            case ConditionOperatorTypes.NotExistsOn:
                description = Resources.AtomicConditionEdit_NotExistsOn;
                break;
            case ConditionOperatorTypes.IsOlderThan:
                description = Resources.AtomicConditionEdit_IsDateBefore;
                break;
            case ConditionOperatorTypes.IsNewerThan:
                description = Resources.AtomicConditionEdit_IsDateAfter;
                break;
            case ConditionOperatorTypes.IsBiggerThan:
                description = Resources.AtomicConditionEdit_IsSizeGreaterThan;
                break;
            case ConditionOperatorTypes.IsSmallerThan:
                description = Resources.AtomicConditionEdit_IsSizeLessThan;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(conditionOperatorType), "unknown ConditionOperatorType " + conditionOperatorType);
        }

        var conditionOperatorView = new ConditionOperatorViewModel(conditionOperatorType, description);
        return conditionOperatorView;
    }

    private void ShowHideControls()
    {
        // Control the visibility of the dot and the SourceTypeComboBox
        IsSourceTypeComboBoxVisible = SelectedSourceOrProperty?.IsDataPart == true;
        IsDotVisible = SelectedSourceOrProperty?.IsDataPart == true;

        // Control the visibility of the DestinationComboBox
        IsDestinationComboBoxVisible = SelectedSourceOrProperty?.IsNameProperty != true;

        // Control the visibility of input fields
        IsDateVisible = SelectedDestination is { IsVirtual: true }
                        && SelectedSourceOrProperty?.IsDataPart == true && SelectedComparisonElement is { ComparisonProperty: ComparisonProperty.Date };
        IsSizeVisible = SelectedDestination is { IsVirtual: true }
                        && SelectedSourceOrProperty?.IsDataPart == true && SelectedComparisonElement is { ComparisonProperty: ComparisonProperty.Size };
        IsNameVisible = SelectedSourceOrProperty?.IsNameProperty == true;
    }

    internal AtomicCondition? ExportAtomicCondition()
    {
        if (SelectedSourceOrProperty == null || SelectedComparisonOperator == null)
        {
            return null;
        }

        // If a property is selected (like Name), use a default source
        DataPart source;
        ComparisonProperty comparisonProperty;
        
        if (SelectedSourceOrProperty.IsProperty)
        {
            // For properties, use the first available source as the default source
            source = _dataPartIndexer.GetAllDataParts().First();
            comparisonProperty = SelectedSourceOrProperty.ComparisonProperty!.Value;
        }
        else
        {
            source = SelectedSourceOrProperty.DataPart!;
            comparisonProperty = SelectedComparisonElement?.ComparisonProperty ?? ComparisonProperty.Content;
        }

        if (SelectedDestination == null || SelectedDestination.IsVirtual)
        {
            if (comparisonProperty == ComparisonProperty.Size && SelectedSize == null)
            {
                return null;
            }
            if (comparisonProperty == ComparisonProperty.Date && SelectedDateTime == null)
            {
                return null;
            }
            if (comparisonProperty == ComparisonProperty.Name && NamePattern.IsNullOrEmpty())
            {
                return null;
            }
        }

        var selectedDestination = SelectedDestination;
        if (selectedDestination is { IsVirtual: true })
        {
            selectedDestination = null;
        }

        var atomicCondition = new AtomicCondition(source, comparisonProperty, 
            SelectedComparisonOperator.ConditionOperator, selectedDestination);

        if (selectedDestination == null && comparisonProperty == ComparisonProperty.Size && SelectedSize != null)
        {
            atomicCondition.Size = SelectedSize;
            atomicCondition.SizeUnit = SelectedSizeUnit.SizeUnit;
        }
        else
        {
            atomicCondition.Size = null;
            atomicCondition.SizeUnit = null;
        }
        
        if (selectedDestination == null && comparisonProperty == ComparisonProperty.Date && SelectedDateTime != null)
        {
            var localDateTime = new DateTime(
                SelectedDateTime.Value.Year, SelectedDateTime.Value.Month, SelectedDateTime.Value.Day,
                SelectedTime.Hours, SelectedTime.Minutes, 0, DateTimeKind.Local);

            atomicCondition.DateTime = localDateTime;
        }
        else
        {
            atomicCondition.DateTime = null;
        }

        if (NamePattern.IsNotEmpty() && comparisonProperty == ComparisonProperty.Name)
        {
            atomicCondition.NamePattern = NamePattern;
        }
        else
        {
            atomicCondition.NamePattern = null;
        }

        return atomicCondition;
    }

    internal void SetAtomicCondition(AtomicCondition atomicCondition)
    {
        // Find the corresponding source or property
        if (atomicCondition.ComparisonProperty == ComparisonProperty.Name)
        {
            // If it is a condition on the name, select the Name property
            SelectedSourceOrProperty = SourceOrProperties.FirstOrDefault(sop => sop.IsNameProperty);
        }
        else
        {
            // Otherwise, select the corresponding source
            SelectedSourceOrProperty = SourceOrProperties.FirstOrDefault(sop => 
                sop.IsDataPart && sop.DataPart?.Name == atomicCondition.Source.Name);
        }

        if (SelectedSourceOrProperty?.IsDataPart == true)
        {
            SelectedComparisonElement = ComparisonProperties.FirstOrDefault(ce => Equals(ce.ComparisonProperty, atomicCondition.ComparisonProperty));
        }
        
        SelectedComparisonOperator = ComparisonOperators.FirstOrDefault(co => Equals(co.ConditionOperator, atomicCondition.ConditionOperator));

        if (atomicCondition.Destination == null)
        {
            SelectedDestination = ConditionDestinations.Single(cd => cd.IsVirtual);
        }
        else
        {
            SelectedDestination = atomicCondition.Destination;
        }
        
        SelectedSize = atomicCondition.Size;
        SelectedSizeUnit = SizeUnits.Single(su => Equals(su.SizeUnit, atomicCondition.SizeUnit ?? Common.Business.Misc.SizeUnits.KB));

        if (atomicCondition.DateTime != null)
        {
            SelectedDateTime = atomicCondition.DateTime.Value.ToLocalTime();
        }
        else
        {
            SelectedDateTime = DateTime.Now;
        }

        SelectedTime = SelectedDateTime.Value.TimeOfDay;

        NamePattern = atomicCondition.NamePattern;
    }

    private void Remove()
    {
        RaiseRemoveRequested();
    }
    
    private void SwapSides()
    {
        var source = SelectedSourceOrProperty;
        var destination = SelectedDestination;

        // Currently, swapping with properties is not allowed
        // because it would require more complex logic
        if (source?.IsDataPart == true && destination is { IsVirtual: false })
        {
            SelectedSourceOrProperty = new SourceOrPropertyViewModel(destination);
            SelectedDestination = source?.DataPart;
        }
    }
}