using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using ByteSync.Views.Misc;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class ComparisonResultViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly IDialogService _dialogService;
    private readonly IInventoryService _inventoryService;
    private readonly IComparisonItemsService _comparisonItemsService;
    private readonly IComparisonItemViewModelFactory _comparisonItemViewModelFactory;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IFlyoutElementViewModelFactory _flyoutElementViewModelFactory;
    private readonly IComparisonItemRepository _comparisonItemRepository;
    private readonly IFilterService _filterService;
    private readonly IWebAccessor _webAccessor;
    private readonly ILogger<ComparisonResultViewModel> _logger;
    
    private const int PAGE_SIZE = 10;
    
    
    public ComparisonResultViewModel()
    {
    }
    
    public ComparisonResultViewModel(ISessionService sessionService, ILocalizationService localizationService,
        IDialogService dialogService, IInventoryService inventoriesService, IComparisonItemsService comparisonItemsService,
        IComparisonItemViewModelFactory comparisonItemViewModelFactory, ISessionMemberRepository sessionMemberRepository,
        IFlyoutElementViewModelFactory flyoutElementViewModelFactory,
        ManageSynchronizationRulesViewModel manageSynchronizationRulesViewModel,
        IComparisonItemRepository comparisonItemRepository, IFilterService filterService, IWebAccessor webAccessor,
        ILogger<ComparisonResultViewModel> logger)
    {
        _sessionService = sessionService;
        _localizationService = localizationService;
        _dialogService = dialogService;
        _inventoryService = inventoriesService;
        _comparisonItemsService = comparisonItemsService;
        _comparisonItemViewModelFactory = comparisonItemViewModelFactory;
        _sessionMemberRepository = sessionMemberRepository;
        _flyoutElementViewModelFactory = flyoutElementViewModelFactory;
        _comparisonItemRepository = comparisonItemRepository;
        _filterService = filterService;
        _webAccessor = webAccessor;
        _logger = logger;
        
        ManageSynchronizationRules = manageSynchronizationRulesViewModel;
        
        IsResultLoadingError = false;
        AreResultsLoaded = false;
        
        IsColumn2Visible = true;
        IsColumn3Visible = true;
        IsColumn4Visible = true;
        
        SelectedItems = new ObservableCollection<ComparisonItemViewModel>();
        
        var canAddOrDeleteManualAction = this
            .WhenAnyValue(
                x => x.SelectedItems.Count, x => x.HasSynchronizationStarted,
                (selected, isSyncStard) => !isSyncStard && selected > 0
                                                        && Enumerable.ToHashSet(SelectedItems.Select(i => i.FileSystemType)
                                                            .ToList()).Count == 1)
            .ObserveOn(RxApp.MainThreadScheduler);
        
        AddManualActionCommand = ReactiveCommand.Create(AddManualAction, canAddOrDeleteManualAction);
        DeleteManualActionsCommand = ReactiveCommand.Create(DeleteManualActions, canAddOrDeleteManualAction);
        OpenSyntaxDocumentationCommand = ReactiveCommand.CreateFromTask(OpenSyntaxDocumentation);
        
        FilterTags = new ObservableCollection<TagItem>();
        
        // Configure tag validation (avoid duplicates, empty words, etc.)
        TagFilterValidator = tag => !string.IsNullOrWhiteSpace(tag) && tag.Length >= 2;
        
        // Observe changes in tags and update the FilterText
        var filter = FilterTags
            .ToObservableChangeSet()
            .AutoRefresh(t => t.Text)
            .StartWithEmpty()
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(_ => BuildFilter());
        
        PageParameters.WhenAnyValue(vm => vm.PageCount)
            .Subscribe(pageCount =>
            {
                if (pageCount == 1)
                {
                    GridMinHeight = null;
                }
                else
                {
                    // Here, it is shorter than the height when 20 items are displayed
                    // When more items are displayed, the height gradually returns, but it still works
                    GridMinHeight = 805;
                }
            });
        
        var pager = PageParameters.WhenAnyValue(vm => vm.CurrentPage, vm => vm.PageSize,
                (currentPage, pageSize) => new PageRequest(currentPage, pageSize))
            .StartWith(new PageRequest(1, PAGE_SIZE))
            .DistinctUntilChanged()
            .Sample(TimeSpan.FromMilliseconds(100));
        
        _comparisonItemRepository.ObservableCache
            .Connect() // make the source an observable change set
            .Filter(filter)
            .Sort(SortExpressionComparer<ComparisonItem>.Ascending(c => c.PathIdentity.LinkingKeyValue))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Page(pager)
            .Do(changes => PageParameters.Update(changes.Response))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(comparisonItem => _comparisonItemViewModelFactory.Create(comparisonItem))
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _bindingData)
            .Subscribe();
        
        this.WhenActivated(disposables =>
        {
            _comparisonItemsService.ComparisonResult.DistinctUntilChanged()
                .Where(comparisonResult => comparisonResult != null)
                .Subscribe(HandleOnInventoriesComparisonDone)
                .DisposeWith(disposables);
            
            _sessionService.SessionStatusObservable
                .Where(s => s.In(SessionStatus.Preparation))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnSessionReset())
                .DisposeWith(disposables);
            
            HandleActivation(disposables);
            
            IsEditionEnabled = true;
        });
    }
    
    private ReadOnlyObservableCollection<ComparisonItemViewModel> _bindingData;
    
    public PageParameterData PageParameters { get; } = new(1, PAGE_SIZE);
    
    public ViewModelBase? ManageSynchronizationRules { get; }
    
    private void HandleActivation(CompositeDisposable compositeDisposable)
    {
        _inventoryService.InventoryProcessData.AreFullInventoriesComplete
            .Where(b => b)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                CanManageActions = !_sessionService.IsCloudSession || _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;
                
                IsCloudProfileSession = _sessionService is { IsProfileSession: true, IsCloudSession: true };
            })
            .DisposeWith(compositeDisposable);
    }
    
    public ReadOnlyObservableCollection<ComparisonItemViewModel> ComparisonItems => _bindingData;
    
    public ReactiveCommand<Unit, Unit> AddManualActionCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> DeleteManualActionsCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> OpenSyntaxDocumentationCommand { get; }
    
    [Reactive]
    public string? PathHeader { get; set; }
    
    [Reactive]
    public string? Inventory1Name { get; set; }
    
    [Reactive]
    public string? Inventory2Name { get; set; }
    
    [Reactive]
    public string? Inventory3Name { get; set; }
    
    [Reactive]
    public string? Inventory4Name { get; set; }
    
    [Reactive]
    public string? Inventory5Name { get; set; }
    
    [Reactive]
    public bool IsColumn2Visible { get; set; }
    
    [Reactive]
    public bool IsColumn3Visible { get; set; }
    
    [Reactive]
    public bool IsColumn4Visible { get; set; }
    
    [Reactive]
    public bool IsColumn5Visible { get; set; }
    
    [Reactive]
    public bool IsEditionEnabled { get; set; }
    
    [Reactive]
    internal ObservableCollection<ComparisonItemViewModel> SelectedItems { get; set; }
    
    [Reactive]
    public bool HasSynchronizationStarted { get; set; }
    
    [Reactive]
    public int? GridMinHeight { get; set; }
    
    [Reactive]
    public bool CanManageActions { get; set; }
    
    [Reactive]
    public bool IsCloudProfileSession { get; set; }
    
    [Reactive]
    public bool AreResultsLoaded { get; set; }
    
    [Reactive]
    public bool IsResultLoadingError { get; set; }
    
    private List<ComparisonItemViewModel> SelectedComparisonItemViews
    {
        get { return SelectedItems.ToList(); }
    }
    
    private async void HandleOnInventoriesComparisonDone(ComparisonResult? comparisonResult)
    {
        try
        {
            IsResultLoadingError = false;
            
            SetColumnsNameAndVisibility(comparisonResult!);
            
            AreResultsLoaded = true;
        }
        catch (Exception ex)
        {
            IsResultLoadingError = true;
            
            _logger.LogError(ex, "ComparisonResultViewModel.HandleOnInventoriesComparisonDone");
        }
    }
    
    private void SetColumnsNameAndVisibility(ComparisonResult comparisonResult)
    {
        var pathHeader = _sessionService.CurrentSessionSettings!.DataType switch
        {
            DataTypes.Files => _localizationService[nameof(Resources.General_File)],
            DataTypes.Directories => _localizationService[nameof(Resources.General_Directory)],
            _ => $"{_localizationService[nameof(Resources.General_File)]} " +
                 $"{_localizationService[nameof(Resources.General_or)]} {_localizationService[nameof(Resources.General_Directory)]}"
        };
        PathHeader = pathHeader.ToUpper();
        
        Inventory1Name = ComputeColumnInventoryDescription(0, comparisonResult);
        
        IsColumn2Visible = comparisonResult.Inventories.Count > 1;
        if (comparisonResult.Inventories.Count > 1)
        {
            Inventory2Name = ComputeColumnInventoryDescription(1, comparisonResult);
        }
        
        IsColumn3Visible = comparisonResult.Inventories.Count > 2;
        if (comparisonResult.Inventories.Count > 2)
        {
            Inventory3Name = ComputeColumnInventoryDescription(2, comparisonResult);
        }
        
        IsColumn4Visible = comparisonResult.Inventories.Count > 3;
        if (comparisonResult.Inventories.Count > 3)
        {
            Inventory4Name = ComputeColumnInventoryDescription(3, comparisonResult);
        }
        
        IsColumn5Visible = comparisonResult.Inventories.Count > 4;
        if (comparisonResult.Inventories.Count > 4)
        {
            Inventory5Name = ComputeColumnInventoryDescription(4, comparisonResult);
        }
    }
    
    private string ComputeColumnInventoryDescription(int inventoryIndex, ComparisonResult comparisonResult)
    {
        var inventoryWord = _localizationService[nameof(Resources.ComparisonResult_Inventory)];
        
        var inventory = comparisonResult.Inventories[inventoryIndex];
        var description = comparisonResult.Inventories.Count == 1 ? string.Empty : inventory.MachineName;
        
        return string.IsNullOrEmpty(description)
            ? $"{inventoryWord} {inventory.Code}"
            : $"{inventoryWord} {inventory.Code} ({description})";
    }
    
    private void AddManualAction()
    {
        _dialogService.ShowFlyout(nameof(Resources.Shell_CreateTargetedAction), false,
            _flyoutElementViewModelFactory.BuildTargetedActionGlobalViewModel(
                SelectedComparisonItemViews.Select(civm => civm.ComparisonItem).ToList()));
    }
    
    private void DeleteManualActions()
    {
        foreach (var comparisonItemView in SelectedComparisonItemViews)
        {
            comparisonItemView.ClearTargetedActions();
        }
    }
    
    private async Task OpenSyntaxDocumentation()
    {
        Dictionary<string, string> pathPerLanguage = new()
        {
            { "en", "/synchronization/filtering-syntax/" },
            { "fr", "/synchronisation/syntaxe-de-filtrage/" }
        };
        
        await _webAccessor.OpenDocumentationUrl(pathPerLanguage);
    }
    
    private void OnSessionReset()
    {
        IsResultLoadingError = false;
        AreResultsLoaded = false;
        HasSynchronizationStarted = false;
        
        IsEditionEnabled = true;
    }
    
    private Func<ComparisonItem, bool> BuildFilter()
    {
        if (FilterTags.Count == 0)
        {
            return _ => true;
        }
        
        var tags = FilterTags
            .Select(t => t.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
        
        return _filterService.BuildFilter(tags);
    }
    
    [Reactive]
    public ObservableCollection<TagItem> FilterTags { get; set; }
    
    // Property to configure the tag autocomplete
    [Reactive]
    public Func<string, bool> TagFilterValidator { get; set; }
}