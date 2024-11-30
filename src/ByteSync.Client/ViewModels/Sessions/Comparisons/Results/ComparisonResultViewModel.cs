using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Arguments;
using ByteSync.Business.Sessions;
using ByteSync.Business.Themes;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Comparisons.Results;

public class ComparisonResultViewModel : ActivableViewModelBase 
{
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    // private readonly IUXEventsHub _uxEventsHub;
    private readonly ILocalizationService _localizationService;
    private readonly IDialogService _dialogService;
    private readonly IInventoryService _inventoryService;
    private readonly IComparisonItemsService _comparisonItemsService;
    private readonly IThemeService _themeService;
    private readonly IComparisonItemViewModelFactory _comparisonItemViewModelFactory;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IFlyoutElementViewModelFactory _flyoutElementViewModelFactory;

    private const int PAGE_SIZE = 10;


    public ComparisonResultViewModel()
    {
    }

    public ComparisonResultViewModel(ISessionService sessionService, ICloudSessionEventsHub cloudSessionEventsHub, ILocalizationService localizationService, 
        IDialogService dialogService, IInventoryService inventoriesService, IComparisonItemsService comparisonItemsService, IThemeService themeService, 
        IComparisonItemViewModelFactory comparisonItemViewModelFactory, ISessionMemberRepository sessionMemberRepository, 
        IFlyoutElementViewModelFactory flyoutElementViewModelFactory, ManageSynchronizationRulesViewModel manageSynchronizationRulesViewModel)
    {
        _sessionService = sessionService;
        _cloudSessionEventsHub = cloudSessionEventsHub;
        // _uxEventsHub = uxEventsHubs ?? Locator.Current.GetService<IUXEventsHub>()!;
        _localizationService = localizationService;
        _dialogService = dialogService;
        _inventoryService = inventoriesService;
        _comparisonItemsService = comparisonItemsService;
        _themeService = themeService;
        _comparisonItemViewModelFactory = comparisonItemViewModelFactory;
        _sessionMemberRepository = sessionMemberRepository;
        _flyoutElementViewModelFactory = flyoutElementViewModelFactory;
        
        ManageSynchronizationRules = manageSynchronizationRulesViewModel;

        // SynchronizationDataHelper = new SynchronizationDataHelper();

        IsResultLoadingError = false;
        AreResultsLoaded = false;
        
        IsColumnBVisible = true;
        IsColumnCVisible = true;
        IsColumnDVisible = true;
        
        SelectedItems = new ObservableCollection<ComparisonItemViewModel>();

        // SharedAtomicActions = new ObservableCollection<SharedAtomicAction>();

        // var canGoPreviousPage = this.WhenAnyValue(x => x.PageIndex, x => x.PageCount, (pageIndex, pageCount) => pageIndex > 1);
        // FirstPageCommand = ReactiveCommand.Create(FirstPage, canGoPreviousPage);
        // PreviousPageCommand = ReactiveCommand.Create(PreviousPage, canGoPreviousPage);
        //
        // var canGoNextPage = this.WhenAnyValue(x => x.PageIndex, x => x.PageCount, (pageIndex, pageCount) => pageIndex < pageCount);
        // NextPageCommand = ReactiveCommand.Create(NextPage, canGoNextPage);
        // LastPageCommand = ReactiveCommand.Create(LastPage, canGoNextPage);
            
        // https://www.reactiveui.net/docs/handbook/when-any/
        // https://www.reactiveui.net/
        // Ceci permet de mettre à jour les éléments affichés lorsque le filtre est changé
        // Permet l'appel à TextFilter
        // this.WhenAnyValue(x => x.FilterText)
        //     .Throttle(TimeSpan.FromSeconds(.35), RxApp.TaskpoolScheduler)
        //     .Select(query => query?.Trim())
        //     .DistinctUntilChanged()
        //     .ObserveOn(RxApp.MainThreadScheduler)
        //     .Subscribe(_ => ComparisonItems?.Refresh());

        var canAddOrDeleteManualAction = this
            .WhenAnyValue(
                x => x.SelectedItems.Count, x=> x.HasSynchronizationStarted,
                (selected, isSyncStard) => !isSyncStard && selected > 0 
                                                        && Enumerable.ToHashSet(SelectedItems.Select(i => i.FileSystemType)
                                                                .ToList()).Count == 1)
            .ObserveOn(RxApp.MainThreadScheduler);

        AddManualActionCommand = ReactiveCommand.Create(AddManualAction, canAddOrDeleteManualAction);
        DeleteManualActionsCommand = ReactiveCommand.Create(DeleteManualActions, canAddOrDeleteManualAction);
        

        
        var filter = this.WhenAnyValue(x => x.FilterText)
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Select(BuildFilter);
            
        // var pager = PageParameters.WhenChanged(vm=>vm.PageSize, vm=>vm.CurrentPage, (_,size, pge) => new PageRequest(pge, size))
        //     .StartWith(new PageRequest(1, 25))
        //     .DistinctUntilChanged()
        //     .Sample(TimeSpan.FromMilliseconds(100));

        PageParameters.WhenAnyValue(vm => vm.PageCount)
            .Subscribe(pageCount =>
            {
                if (pageCount == 1)
                {
                    GridMinHeight = null;
                }
                else
                {
                    // Ici, c'est moins haut que la hauteur quand 20 éléments sont affichés
                    // Quand plus d'éléments sont affichés, la hauteur revient progressivement mais cela fonctionne quand même
                    GridMinHeight = 805;
                }
            });
        
        var pager = PageParameters.WhenAnyValue(vm => vm.CurrentPage, vm => vm.PageSize, 
                (currentPage, pageSize) => new PageRequest(currentPage, pageSize))
            .StartWith(new PageRequest(1, PAGE_SIZE))
            .DistinctUntilChanged()
            .Sample(TimeSpan.FromMilliseconds(100));

        _comparisonItemsService.ComparisonItems
            .Connect() // make the source an observable change set
            .Filter(filter)
            // .Transform(comparisonItem => _comparisonItemViewModelFactory.CreateSynchronizationActionViewModel(comparisonItem))
            .Sort(SortExpressionComparer<ComparisonItem>.Ascending(c => c.PathIdentity.LinkingKeyValue))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Page(pager)
            .Do(changes => PageParameters.Update(changes.Response))
            .Transform(comparisonItem => _comparisonItemViewModelFactory.Create(comparisonItem))
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            // .Do(changes => PageParameters.Update(changes.Response))
            // Make sure this line^^ is only right before the Bind()
            // This may be important to avoid threading issues if
            // 'mySource' is updated on a different thread.
            .Bind(out _bindingData)
            // .DisposeMany()
            .Subscribe();
            // .DisposeWith(disposables);
        
        this.WhenActivated(disposables =>
        {
            _comparisonItemsService.ComparisonResult.DistinctUntilChanged()
                .Where(comparisonResult => comparisonResult != null)
                .Subscribe(HandleOnInventoriesComparisonDone)
                .DisposeWith(disposables);
            
            // _comparisonItemsService.ComparisonResult.DistinctUntilChanged()
            //     .CombineLatest(
            //         Observable
            //             .FromEventPattern<PropertyChangedEventArgs>(_localizationService, nameof(_localizationService.PropertyChanged))
            //             .StartWith(default(EventPattern<PropertyChangedEventArgs>)))
            //     .Where(tuple => tuple.First != null)
            //     // .Where(comparisonResult => comparisonResult != null)
            //     .Select(tuple => tuple.First)
            //     .Subscribe(HandleOnInventoriesComparisonDone)
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<GenericEventArgs<ComparisonResult>>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.InventoriesComparisonDone))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnInventoriesComparisonDone(evt.EventArgs.Value))
            //     .DisposeWith(disposables);
            

            
            // Observable.FromEventPattern<SynchronizationStartedEventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SynchronizationStarted))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnSynchronizationStarted(evt.EventArgs))
            //     .DisposeWith(disposables);
            
            // _synchronizationService.SynchronizationProcessData.SynchronizationStart
            //     .Where(ss => ss != null)
            //     .Subscribe(ss => OnSynchronizationStarted(ss!))
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<GenericEventArgs<SynchronizationProgressInfo>>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SynchronizationProgressChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnSynchronizationProgressChanged(evt.EventArgs))
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<GenericEventArgs<SynchronizationActionViewModel>>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SynchronizationActionRemoved))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnSynchronizationActionRemoved(evt.EventArgs))
            //     .DisposeWith(disposables);
            
            _sessionService.SessionStatusObservable
                .Where(s => s.In(SessionStatus.Preparation))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnSessionResetted())
                .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SessionResetted))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnSessionResetted())
            //     .DisposeWith(disposables);
            
            _themeService.SelectedTheme
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnThemeChanged)
                .DisposeWith(disposables);
            
            // Observable.FromEventPattern<ThemeEventArgs>(_uxEventsHub, nameof(_uxEventsHub.ThemeChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnThemeChanged(evt.EventArgs))
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<PropertyChangedEventArgs>(_localizationService, nameof(_localizationService.PropertyChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnLocaleChanged())
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<GenericEventArgs<SynchronizationRule>>(_actionEditionEventsHub, nameof(_actionEditionEventsHub.SynchronizationRuleSaved))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnSynchronizationRuleSaved(evt.EventArgs.Value))
            //     .DisposeWith(disposables);
            
            // this.WhenAnyValue(x => x.FilterText)
            //     .Throttle(TimeSpan.FromSeconds(.35), RxApp.TaskpoolScheduler)
            //     .Select(query => query?.Trim())
            //     .DistinctUntilChanged()
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => ComparisonItems?.Refresh());
            
            
            /*
            var filter = this.WhenAnyValue(x => x.FilterText)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Select(BuildFilter);
            
            var pager = PageParameters.WhenChanged(vm=>vm.PageSize,vm=>vm.CurrentPage, (_,size, pge) => new PageRequest(pge, size))
                .StartWith(new PageRequest(1, 25))
                .DistinctUntilChanged()
                .Sample(TimeSpan.FromMilliseconds(100));
            
            _comparisonItemsService.ComparisonItems
                .Connect() // make the source an observable change set
                .Transform(comparisonItem => new ComparisonItemViewModel(comparisonItem, comparisonItem.ComparisonResult.Inventories))
                // .Filter(filter)
                // .Sort(SortExpressionComparer<ComparisonItemViewModel>.Ascending(c => c.PathIdentity.LinkingKeyValue))
                // .Page(pager)
                .ObserveOn(RxApp.MainThreadScheduler) 
                // .Do(changes => PageParameters.Update(changes.Response))
                // Make sure this line^^ is only right before the Bind()
                // This may be important to avoid threading issues if
                // 'mySource' is updated on a different thread.
                .Bind(out _bindingData)
                .Subscribe()
                .DisposeWith(disposables);
                */
            
            // .Sort(SortExpressionComparer<ComparisonItemViewModel>.Ascending(c => c.PathIdentity.PathIdentity.LinkingKeyValue))
            
            // var disposable = _comparisonItemsService.ComparisonItems
            //     .Connect() // make the source an observable change set
            //     .CombineLatest(_comparisonItemsService.ComparisonResult)
            //     .Where(tuple => tuple.Second != null)
            //     .Select(tuple => new ComparisonItemViewModel(tuple.First., tuple.Second?.Inventories))
            //     .Sort(SortExpressionComparer<ComparisonItem>.Ascending(t => t.PathIdentity.LinkingKeyValue))
            //     .ObserveOn(RxApp.MainThreadScheduler) 
            //     // Make sure this line^^ is only right before the Bind()
            //     // This may be important to avoid threading issues if
            //     // 'mySource' is updated on a different thread.
            //     .Bind(out _bindingData)
            //     .Subscribe()
            //     .DisposeWith(disposables);

            this.HandleActivation(disposables);
            
            /*
            ComparisonItems = new DataGridCollectionView(_bindingData);
            ComparisonItems.Filter = TextFilter;
            ComparisonItems.PageSize = PAGE_SIZE;

            ComparisonItems.PageChanged += ComparisonItems_OnPageChanged;
            ComparisonItems.PropertyChanged += ComparisonItems_OnPropertyChanged;

            GetPagesValues();*/

            IsEditionEnabled = true;
        });

       

        // DataHasBeenLoadedOnce = false;
    }
    
    private ReadOnlyObservableCollection<ComparisonItemViewModel> _bindingData;

    public PageParameterData PageParameters { get; } = new PageParameterData(1, PAGE_SIZE);

    // private SynchronizationDataHelper SynchronizationDataHelper { get; }
    
    public ViewModelBase? ManageSynchronizationRules { get; }

    private void HandleActivation(System.Reactive.Disposables.CompositeDisposable compositeDisposable)
    {
        _inventoryService.InventoryProcessData.AreFullInventoriesComplete
            .Where(b => b)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                // if (_sessionDataHolder.IsInventoriesComparisonDone)
                // {
                //     OnInventoriesComparisonDone(_sessionDataHolder.ComparisonResult!);
                // }
            
                // SynchronizationRules = _synchronizationRulesService.SynchronizationRules;

                // SharedAtomicActions = _synchronizationActionsService.SharedAtomicActions2!;

                CanManageActions = ! _sessionService.IsCloudSession || _sessionMemberRepository.IsCurrentUserFirstSessionMemberCurrentValue;

                IsCloudProfileSession = _sessionService is { IsProfileSession: true, IsCloudSession: true };
            })
            .DisposeWith(compositeDisposable);
        
        // if (_sessionDataHolder.AreFullInventoriesComplete)
        // {
        //     if (_sessionDataHolder.IsInventoriesComparisonDone)
        //     {
        //         OnInventoriesComparisonDone(_sessionDataHolder.ComparisonResult!);
        //     }
        //     
        //     SynchronizationRules = _sessionDataHolder.SynchronizationRules;
        //
        //     SharedAtomicActions = _sessionDataHolder.SharedAtomicActions!;
        //
        //     CanManageActions = ! _sessionDataHolder.IsCloudSession || _sessionDataHolder.IsCloudSessionCreatedByMe;
        //
        //     IsCloudProfileSession = _sessionDataHolder is { IsProfileSession: true, IsCloudSession: true };
        // }
    }

    /*
    private void ComparisonItems_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // On écoute OnPageChanged, OnPropertyChanged/ItemCount et OnPropertyChanged/Count
        // Pour mettre à jour les propriétés PageIndex et PageCount
            
        if (Equals(e.PropertyName, nameof(ComparisonItems.ItemCount)) 
            || Equals(e.PropertyName, nameof(ComparisonItems.Count)))
        {
            GetPagesValues();
        }
    }

    private void ComparisonItems_OnPageChanged(object? sender, EventArgs e)
    {
        // On écoute OnPageChanged, OnPropertyChanged/ItemCount et OnPropertyChanged/Count
        // Pour mettre à jour les propriétés PageIndex et PageCount
            
        GetPagesValues();
    }

    private void GetPagesValues()
    {
        PageIndex = ComparisonItems.PageIndex + 1;
        PageCount = ReflectionUtils.GetPrivatePropertyValue<int>(ComparisonItems, "PageCount");
    }*/

    /*
    private async Task<List<ComparisonItemViewModel>> BuildComparisonItemViewModelList(ComparisonResult comparisonResult)
    {
        return await Task.Run(() =>
        {
            List<ComparisonItemViewModel> list = new List<ComparisonItemViewModel>();
            foreach (var resultComparisonItem in comparisonResult.ComparisonItems.OrderBy(c => c.PathIdentity.LinkingKeyValue))
            {
                var comparisonItemView = new ComparisonItemViewModel(resultComparisonItem, comparisonResult.Inventories);

                list.Add(comparisonItemView);
            }
    
            return list;
        });
    }
    */
    
    public ReadOnlyObservableCollection<ComparisonItemViewModel> ComparisonItems => _bindingData;
    // public DataGridCollectionView ComparisonItems { get; private set; }

    // public ReactiveCommand<Unit, Unit> FirstPageCommand { get; set; }
    //
    // public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; set; }
    //
    // public ReactiveCommand<Unit, Unit> NextPageCommand { get; set; }
    //
    // public ReactiveCommand<Unit, Unit> LastPageCommand { get; set; }

    public ReactiveCommand<Unit, Unit> AddManualActionCommand { get; set; }

    public ReactiveCommand<Unit, Unit> DeleteManualActionsCommand { get; set; }
    


    // public ReactiveCommand<Unit, Unit> RunActionsCommand { get; set; }
    
    [Reactive]
    public string? FilterText { get; set; }
    
    [Reactive]
    public string? PathHeader { get; set; }

    [Reactive]
    public string? InventoryAName { get; set; }

    [Reactive]
    public string? InventoryBName { get; set; }

    [Reactive]
    public string? InventoryCName { get; set; }

    [Reactive]
    public string? InventoryDName { get; set; }

    [Reactive]
    public string? InventoryEName { get; set; }

    [Reactive]
    public bool IsColumnBVisible { get; set; }
    
    [Reactive]
    public bool IsColumnCVisible { get; set; }

    [Reactive]
    public bool IsColumnDVisible { get; set; }

    [Reactive]
    public bool IsColumnEVisible { get; set; }

    [Reactive]
    public bool IsEditionEnabled { get; set; }
        
    [Reactive]
    public int PageIndex { get; set; }
        
    [Reactive]
    public int PageCount { get; set; }
        
    [Reactive]
    internal ObservableCollection<ComparisonItemViewModel> SelectedItems { get; set; }
        
    // [Reactive]
    // public ObservableCollection<SharedAtomicAction> SharedAtomicActions { get; set; }
        
    // [Reactive]
    // internal ObservableCollection<SynchronizationRuleSummaryViewModel>? SynchronizationRules { get; set; }
    
    // public bool DataHasBeenLoadedOnce { get; set; }

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
        get
        {
            // if (SelectedItems == null)
            // {
            //     return new List<ComparisonItemViewModel>();
            // }
            // else
            // {
            //     return SelectedItems.Cast<ComparisonItemViewModel>().ToList();
            // }
            
            return SelectedItems.ToList();
        }
    }

    // private void OnInventoriesComparisonDone(ComparisonResult comparisonResult)
    // {
    //     Dispatcher.UIThread.InvokeAsync(() =>
    //     {
    //         HandleOnInventoriesComparisonDone(comparisonResult);
    //     });
    // }

    private async void HandleOnInventoriesComparisonDone(ComparisonResult? comparisonResult)
    {
        try
        {
            IsResultLoadingError = false;
            
            // if (DataHasBeenLoadedOnce)
            // {
            //     return;
            // }
            //
            // DataHasBeenLoadedOnce = true;

            // InitializeComparisonItems();
            
            SetColumnsNameAndVisibility(comparisonResult!);

            // await _comparisonItemsService.InitializeComparisonItems();
            
            // var list = await BuildComparisonItemViewModelList(comparisonResult);
            //
            // var disposable = _sessionDataHolder.ComparisonItems.SuspendNotifications();
            // _sessionDataHolder.ComparisonItems.AddRange(list);
            // disposable.Dispose();

            // if (PageCount == 1)
            // {
            //     GridMinHeight = null;
            // }
            // else
            // {
            //     // Ici, c'est moins haut que la hauteur quand 20 éléments sont affichés
            //     // Quand plus d'éléments sont affichés, la hauteur revient progressivement mais cela fonctionne quand même
            //     GridMinHeight = 805;
            // }

            // _sessionDataHolder.ApplySynchronizationRules();

            AreResultsLoaded = true;
        }
        catch (Exception ex)
        {
            IsResultLoadingError = true;
            
            Log.Error(ex, "ComparisonResultViewModel.HandleOnInventoriesComparisonDone");
        }
        
        // foreach (ComparisonItemViewModel comparisonItem in ComparisonItems)
        // {
        //     comparisonItem?.OnLocaleChanged(_localizationService);
        // }
        
        
        // todo 040423
        // foreach (var synchronizationRuleSummaryViewModel in SynchronizationRules!)
        // {
        //     synchronizationRuleSummaryViewModel.OnLocaleChanged();
        // }
    }

    /*
    private void InitializeComparisonItems()
    {
        ComparisonItems = new DataGridCollectionView(_bindingData);
        ComparisonItems.Filter = TextFilter;
        ComparisonItems.PageSize = PAGE_SIZE;

        ComparisonItems.PageChanged += ComparisonItems_OnPageChanged;
        ComparisonItems.PropertyChanged += ComparisonItems_OnPropertyChanged;
        GetPagesValues();
    }*/

    private void SetColumnsNameAndVisibility(ComparisonResult comparisonResult)
    {
        var inventoryWord = _localizationService[nameof(Resources.ComparisonResult_Inventory)];
        
        var pathHeader = _sessionService.CurrentSessionSettings!.DataType switch
        {
            DataTypes.Files => _localizationService[nameof(Resources.General_File)],
            DataTypes.Directories => _localizationService[nameof(Resources.General_Directory)],
            _ => $"{_localizationService[nameof(Resources.General_File)]} " +
                 $"{_localizationService[nameof(Resources.General_or)]} {_localizationService[nameof(Resources.General_Directory)]}"
        };
        PathHeader = pathHeader.ToUpper();

        InventoryAName = $"{inventoryWord} A ({ComputeColumnInventoryDescription(0, comparisonResult)})";

        IsColumnBVisible = comparisonResult.Inventories.Count > 1;
        if (comparisonResult.Inventories.Count > 1)
        {
            InventoryBName = $"{inventoryWord} B ({ComputeColumnInventoryDescription(1, comparisonResult)})";
        }

        IsColumnCVisible = comparisonResult.Inventories.Count > 2;
        if (comparisonResult.Inventories.Count > 2)
        {
            InventoryCName = $"{inventoryWord} C ({ComputeColumnInventoryDescription(2, comparisonResult)})";
        }

        IsColumnDVisible = comparisonResult.Inventories.Count > 3;
        if (comparisonResult.Inventories.Count > 3)
        {
            InventoryDName = $"{inventoryWord} D ({ComputeColumnInventoryDescription(3, comparisonResult)})";
        }

        IsColumnEVisible = comparisonResult.Inventories.Count > 4;
        if (comparisonResult.Inventories.Count > 4)
        {
            InventoryEName = $"{inventoryWord} E ({ComputeColumnInventoryDescription(3, comparisonResult)})";
        }
    }
    
    private string ComputeColumnInventoryDescription(int inventoryIndex, ComparisonResult comparisonResult)
    {
        if (_sessionService.IsCloudSession)
        {
        #if DEBUG
            if (Environment.GetCommandLineArgs().Contains(DebugArguments.SHOW_DEMO_DATA))
            {
                var result = $"MACHINE_NAME_{inventoryIndex + 1}";
                return result;
            }
        #endif
                
            return comparisonResult.Inventories[inventoryIndex].MachineName;
        }
        else
        {
            // return comparisonResult.Inventories[inventoryIndex].InventoryParts[0].RootName
            //     .Trim('\\', '/');
                
            var result = comparisonResult.Inventories[inventoryIndex].InventoryParts[0].RootPath
                .Trim('\\', '/');
                
            // ici, on pourra introduire une méthode / classe en charge de gérer le namining si besoin
            const int maxLength = 30;
            if (result.Length > maxLength)
            {
                // int remainingLength = maxLength - 9 - 3;
                // result = result.Substring(0, 9) + "..." + result.Substring(result.Length - remainingLength, remainingLength);
                result = result.Substring(0, 12).Trim() + "..." + comparisonResult.Inventories[inventoryIndex].InventoryParts[0].RootName;
            }

            return result;
        }
    }

    // Anciennement utilisé : https://stackoverflow.com/questions/9880589/bind-to-selecteditems-from-datagrid-or-listbox-in-mvvm
    private void AddManualAction()
    {
        _dialogService.ShowFlyout(nameof(Resources.Shell_CreateTargetedAction), false,
            _flyoutElementViewModelFactory.BuildTargetedActionGlobalViewModel(
                SelectedComparisonItemViews.Select(civm => civm.ComparisonItem).ToList()));
        
        // _navigationEventsHub.RaiseManualActionCreationRequested(this, SelectedComparisonItemViews);
    }

    private void DeleteManualActions()
    {
        foreach (var comparisonItemView in SelectedComparisonItemViews)
        {
            comparisonItemView.ClearTargetedActions();
        }

        // _synchronizationActionsService.UpdateSharedSynchronizationActions();
    }

    // private void OnSynchronizationRuleSaved(SynchronizationRule synchronizationRule)
    // {
    //     var automaticActionSummaryViewModel
    //         = SynchronizationRules!.FirstOrDefault(vm =>Equals(vm.SynchronizationRule, synchronizationRule));
    //
    //     if (automaticActionSummaryViewModel == null)
    //     {
    //         automaticActionSummaryViewModel = _synchronizationRuleSummaryViewModelFactory.Create(synchronizationRule); // new SynchronizationRuleSummaryViewModel(synchronizationRule);
    //         SynchronizationRules!.Add(automaticActionSummaryViewModel);
    //     }
    //     else
    //     {
    //         automaticActionSummaryViewModel.UpdateAutomaticAction(synchronizationRule);
    //     }
    //
    //     _comparisonItemsService.ApplySynchronizationRules();
    // }



    // private void OnSynchronizationProgressChanged(GenericEventArgs<SynchronizationProgressInfo> eventArgs)
    // {
    //     SynchronizationDataHelper.OnSynchronizationProgressChanged(eventArgs.Value);
    // }

    // private void OnSynchronizationActionRemoved(GenericEventArgs<SynchronizationActionViewModel> eventArgs)
    // {
    //     // todo 040423
    //     
    //     /*foreach (ComparisonItemViewModel comparisonItem in ComparisonItems.SourceCollection)
    //     {
    //         comparisonItem?.OnSynchronizationActionRemoved(eventArgs.Value);
    //         
    //         SharedAtomicActions.RemoveAll(saa => saa.AtomicActionId.Equals(eventArgs.Value.AtomicAction.AtomicActionId));
    //     }
    //     */
    // }
    
    
    private void OnThemeChanged(Theme? theme)
    {
        // Si trop lent avec ComparisonItems.SourceCollection, on pourrait faire :
        //  - Pour ComparisonItems: MAJ en Reactive
        //  - Pour ComparisonItems.SourceCollection: MAJ en direct sur la sous propriété
        foreach (var comparisonItem in ComparisonItems)
        {
            comparisonItem?.OnThemeChanged();
        }
    }
    
    private void OnSessionResetted()
    {
        // DataHasBeenLoadedOnce = false;
        IsResultLoadingError = false;
        AreResultsLoaded = false;
        HasSynchronizationStarted = false;
        
        
        // todo 090523
        // foreach (var synchronizationRuleSummaryViewModel in SynchronizationRules!)
        // {
        //     synchronizationRuleSummaryViewModel.OnSessionResetted();
        // }

        // SynchronizationDataHelper.Reset();
        
        // SynchronizationRules?.Clear();

        IsEditionEnabled = true;
    }

    /*
    private void OnLocaleChanged()
    {
        
        // foreach (ComparisonItemViewModel comparisonItem in ComparisonItems.SourceCollection)
        // {
        //     comparisonItem?.OnLocaleChanged(_localizationService);
        // }
        //
        // foreach (var synchronizationRuleSummaryViewModel in SynchronizationRules!)
        // {
        //     synchronizationRuleSummaryViewModel.OnLocaleChanged();
        // }

        
        // SetColumnsNameAndVisibility();
    }*/

    /*
    private void FirstPage()
    {
        ComparisonItems.MoveToFirstPage();
    }

    private void PreviousPage()
    {
        ComparisonItems.MoveToPreviousPage();
    }

    private void NextPage()
    {
        ComparisonItems.MoveToNextPage();
    }

    private void LastPage()
    {
        ComparisonItems.MoveToLastPage();
    }*/

    private Func<ComparisonItem, bool> BuildFilter(string? filterText)
    {
        if (filterText.IsNullOrEmpty())
        {
            return _ => true;
        }

        //return false;

        return comparisonItem =>
        {
            List<string> expressions = Enumerable.ToList(filterText!.Split(" ", StringSplitOptions.RemoveEmptyEntries));

            var advancedExpressions = expressions.Where(e => e.StartsWith(":"));
            var otherExpressions = expressions.Where(e => !e.StartsWith(":")).ToList();

            foreach (var advancedExpression in advancedExpressions)
            {
                if (advancedExpression.Equals(":notok", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (comparisonItem.Status.IsOK)
                    {
                        return false;
                    }
                }
                if (advancedExpression.Equals(":ok", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!comparisonItem.Status.IsOK)
                    {
                        return false;
                    }
                }
                if (advancedExpression.Equals(":file", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (comparisonItem.FileSystemType == FileSystemTypes.Directory)
                    {
                        return false;
                    }
                }
                if (advancedExpression.Equals(":dir", StringComparison.InvariantCultureIgnoreCase) 
                    || advancedExpression.Equals(":directory", StringComparison.InvariantCultureIgnoreCase) )
                {
                    if (comparisonItem.FileSystemType == FileSystemTypes.File)
                    {
                        return false;
                    }
                }
                
                // if (Regex.IsMatch(advancedExpression, ":only(?<inventoryLetter>)[A-Z])") advancedExpression.StartsWith(":onlyA", StringComparison.InvariantCultureIgnoreCase))
                // {
                // }

                if (advancedExpression.StartsWith(":only", StringComparison.InvariantCultureIgnoreCase))
                {
                    var letter = advancedExpression.Substring(":only".Length).ToUpper();
                    
                    var inventories = comparisonItem.ContentIdentities.SelectMany(ci => ci.GetInventories())
                        .ToHashSet();

                    if (inventories.Count != 1 || !inventories.First().Letter.Equals(letter))
                    {
                        return false;
                    }
                    // if (inventories.Count() == 1 && inventories.First().Letter.Equals("A"))
                    // {
                    //     
                    // }
                    //
                    // if (comparisonItemViewModel.ContentIdentities.SelectMany(ci => ci.GetInventories())
                    //         .Get.All(ci => ci.)  .ContentIdentitiesA.Count == 0)
                    // {
                    //     return false;
                    // }
                    // else
                    // {
                    //     if (comparisonItemViewModel.ContentIdentitiesList.Skip(1).Any(ci => ci.Count > 0))
                    //     {
                    //         return false;
                    //     }
                    // }
                }

                if (advancedExpression.StartsWith(":ison", StringComparison.InvariantCultureIgnoreCase))
                {
                    var letter = advancedExpression.Substring(":ison".Length).ToUpper();
                    
                    var inventories = comparisonItem.ContentIdentities.SelectMany(ci => ci.GetInventories())
                        .ToHashSet();

                    if (!inventories.Any(i => i.Letter.Equals(letter)))
                    {
                        return false;
                    }
                }
            }

            if (otherExpressions.Count == 0)
            {
                return true;
            }
            else
            {
                var containsAll = otherExpressions.All(e => comparisonItem.PathIdentity.FileName.Contains(e));

                return containsAll;
            }
        };

        
    }
}