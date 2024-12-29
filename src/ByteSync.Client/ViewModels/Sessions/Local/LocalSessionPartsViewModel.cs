using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Arguments;
using ByteSync.Business.Events;
using ByteSync.Business.Inventories;
using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.Interfaces.Repositories;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace ByteSync.ViewModels.Sessions.Local;

public class LocalSessionPartsViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly IInventoryService _inventoryService;
    private readonly IPathItemsService _pathItemsService;
    private readonly IPathItemProxyFactory _pathItemProxyFactory;
    
    private ReadOnlyObservableCollection<PathItemProxy> _data;
    private readonly IPathItemRepository _pathItemRepository;
    private readonly ILogger<LocalSessionPartsViewModel> _logger;

    public LocalSessionPartsViewModel() 
    {
        
    }
    
    public LocalSessionPartsViewModel(ISessionService sessionService,
        ILocalizationService localizationService, ICloudSessionEventsHub cloudSessionEventsHub,
        IInventoryService inventoriesService, IPathItemsService pathItemsService,
        IPathItemProxyFactory pathItemProxyFactory, IPathItemRepository pathItemRepository, ILogger<LocalSessionPartsViewModel> logger)
    {
        _sessionService = sessionService;
        _localizationService = localizationService;
        _cloudSessionEventsHub = cloudSessionEventsHub;
        _inventoryService = inventoriesService;
        _pathItemsService = pathItemsService;
        _pathItemProxyFactory = pathItemProxyFactory;
        _pathItemRepository = pathItemRepository;
        _logger = logger;

        IsFileSystemSelectionEnabled = true;
        
        // https://stackoverflow.com/questions/58479606/how-do-you-update-the-canexecute-value-after-the-reactivecommand-has-been-declar
        // https://www.reactiveui.net/docs/handbook/commands/
        
        RemovePathItemCommand = ReactiveCommand.CreateFromTask<PathItemProxy>(RemovePathItem);

        // var isCountNotOK = this.WhenAnyValue(x => x.Parts.Count, count => count <= 2 );
        // var isCountOK = this.WhenAnyValue(x => x.Parts, parts => parts.Count < 5 );
        // var isCountOK2 = this.WhenAnyObservable(x => x.Parts, x=>x.AddDirectoryCommand.IsExecuting, 
        //     (parts, isExecuting) => parts.Count < 5 );
        var canRun = new BehaviorSubject<bool>(true);
        AddDirectoryCommand = ReactiveCommand.CreateFromTask(AddDirectory, canRun);
        AddFileCommand= ReactiveCommand.CreateFromTask(AddFile, canRun);

        // https://stackoverflow.com/questions/15474043/reactivecommand-with-combined-criteria-for-canexecute
        // Observable.CombineLatest(
        //         this.WhenAnyValue(x => x.Parts.Count, count => count < 5),
        //         AddDirectoryCommand.IsExecuting,
        //         (p1, p2) => p1 && !p2)
        //     .Subscribe(canRun);
        
        this.WhenActivated(disposables =>
        {
            var countChanged = _pathItemRepository
                .CurrentMemberPathItems
                .CountChanged
                .StartWith(0)
                .Select(count => count >= 5);

            Observable.Merge(AddDirectoryCommand.IsExecuting, AddFileCommand.IsExecuting, 
                    countChanged)
                .Select(executing => !executing).Subscribe(canRun);
            
            _pathItemRepository.CurrentMemberPathItems.Connect()
                .Sort(SortExpressionComparer<PathItem>.Ascending(p => p.Code))
                .Transform(pathItem => _pathItemProxyFactory.CreatePathItemProxy(pathItem))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _data)
                .DisposeMany() // dispose when no longer required
                .Subscribe()
                .DisposeWith(disposables);
        
            _inventoryService.InventoryProcessData.MainStatus.DistinctUntilChanged()
                .Where(status => status == LocalInventoryPartStatus.Running)
                .Subscribe(_ => IsFileSystemSelectionEnabled = false)
                .DisposeWith(disposables);
            
            Observable.FromEventPattern<PropertyChangedEventArgs>(_localizationService, nameof(_localizationService.PropertyChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(evt => OnLocaleChanged())
                .DisposeWith(disposables);

            // Observable.FromEventPattern<InventoryStatusChangedEventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.InventoryStatusChanged))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(evt => OnInventoryStatusChanged(evt.EventArgs))
            //     .DisposeWith(disposables);
        });
        
    #if DEBUG
        if (_sessionService.CurrentRunSessionProfileInfo == null)
        {
            void DebugAddDesktopPathItem(string folderName)
            {
                var newPathItem = new PathItem();
                newPathItem.Path = IOUtils.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), folderName);
                newPathItem.Type = FileSystemTypes.Directory;

                //newPathItem.Code = ((char)('A' + Parts.Count)).ToString();

                //PathItemViewModel partViewModel = new PathItemViewModel(newPathItem, _localizationService);

                //Parts.Add(partViewModel);

                _pathItemsService.AddPathItem(newPathItem);
                // _sessionDataHolder.PathItems!.Add(newPathItem);
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTA))
            {
                DebugAddDesktopPathItem("testA");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTA1))
            {
                DebugAddDesktopPathItem("testA1");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTB))
            {
                DebugAddDesktopPathItem("testB");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTB1))
            {
                DebugAddDesktopPathItem("testB1");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTC))
            {
                DebugAddDesktopPathItem("testC");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTC1))
            {
                DebugAddDesktopPathItem("testC1");
            }
            
            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_PATHITEM_TESTTMP))
            {
                DebugAddDesktopPathItem("testTmp");
            }
        }
    #endif
    }
    
    public ReadOnlyObservableCollection<PathItemProxy> PathItems => _data;
    
    public ReactiveCommand<PathItemProxy, Unit> RemovePathItemCommand { get; set; }

    private void OnLocalInventoryStarted()
    {
        IsFileSystemSelectionEnabled = false;
    }

    private void OnLocaleChanged()
    {

    }
    
    public ReactiveCommand<Unit, Unit> AddDirectoryCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> AddFileCommand { get; set; }
    
    [Reactive]
    public bool IsFileSystemSelectionEnabled { get; set; }
    
    private async Task AddDirectory()
    {
        try
        {
            var fileDialogService = Locator.Current.GetService<IFileDialogService>()!;

            var result = await fileDialogService.ShowOpenFolderDialogAsync(Resources.SessionMachineView_SelectDirectory);

            if (result != null && Directory.Exists(result))
            {
                await _pathItemsService.CreateAndAddPathItem(result, FileSystemTypes.Directory);
                //await HandleNewPathItem(result, FileSystemTypes.Directory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionMachineViewModel.AddDirectory");
        }
    }
    
    private async Task AddFile()
    {
        try
        {
            var fileDialogService = Locator.Current.GetService<IFileDialogService>()!;

            var results = await fileDialogService.ShowOpenFileDialogAsync(Resources.SessionMachineView_SelectFiles, true);

            if (results != null)
            {
                foreach (var result in results)
                {
                    await _pathItemsService.CreateAndAddPathItem(result, FileSystemTypes.File);
                    //await HandleNewPathItem(result, FileSystemTypes.File);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionMachineViewModel.AddDirectory");
        }
    }

    private async Task RemovePathItem(PathItemProxy pathItemProxy)
    {
        await _pathItemsService.RemovePathItem(pathItemProxy.PathItem);
    }
}