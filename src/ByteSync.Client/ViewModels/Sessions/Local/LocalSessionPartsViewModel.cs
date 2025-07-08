using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Arguments;
using ByteSync.Business.DataSources;
using ByteSync.Business.Events;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
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
    private readonly IInventoryService _inventoryService;
    private readonly IDataSourceService _dataSourceService;
    private readonly IDataSourceProxyFactory _dataSourceProxyFactory;
    
    private ReadOnlyObservableCollection<DataSourceProxy> _data;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly ILogger<LocalSessionPartsViewModel> _logger;

    public LocalSessionPartsViewModel() 
    {
        
    }
    
    public LocalSessionPartsViewModel(ISessionService sessionService,
        ILocalizationService localizationService, IInventoryService inventoriesService, IDataSourceService dataSourceService,
        IDataSourceProxyFactory dataSourceProxyFactory, IDataSourceRepository dataSourceRepository, ILogger<LocalSessionPartsViewModel> logger)
    {
        _sessionService = sessionService;
        _localizationService = localizationService;
        _inventoryService = inventoriesService;
        _dataSourceService = dataSourceService;
        _dataSourceProxyFactory = dataSourceProxyFactory;
        _dataSourceRepository = dataSourceRepository;
        _logger = logger;

        IsFileSystemSelectionEnabled = true;
        
        // https://stackoverflow.com/questions/58479606/how-do-you-update-the-canexecute-value-after-the-reactivecommand-has-been-declar
        // https://www.reactiveui.net/docs/handbook/commands/
        
        RemoveDataSourceCommand = ReactiveCommand.CreateFromTask<DataSourceProxy>(RemoveDataSource);

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
            var countChanged = _dataSourceRepository
                .CurrentMemberDataSources
                .CountChanged
                .StartWith(0)
                .Select(count => count >= 5);

            Observable.Merge(AddDirectoryCommand.IsExecuting, AddFileCommand.IsExecuting, 
                    countChanged)
                .Select(executing => !executing).Subscribe(canRun);
            
            _dataSourceRepository.CurrentMemberDataSources.Connect()
                .Sort(SortExpressionComparer<DataSource>.Ascending(p => p.Code))
                .Transform(dataSource => _dataSourceProxyFactory.CreateDataSourceProxy(dataSource))
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
            void DebugAddDesktopDataSource(string folderName)
            {
                var newDataSource = new DataSource();
                newDataSource.Path = IOUtils.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), folderName);
                newDataSource.Type = FileSystemTypes.Directory;

                //newDataSource.Code = ((char)('A' + Parts.Count)).ToString();

                //DataSourceViewModel partViewModel = new DataSourceViewModel(newDataSource, _localizationService);

                //Parts.Add(partViewModel);

                _dataSourceService.TryAddDataSource(newDataSource);
                // _sessionDataHolder.DataSource!.Add(newDataSource);
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_DATASOURCE_TESTA))
            {
                DebugAddDesktopDataSource("testA");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_DATASOURCE_TESTA1))
            {
                DebugAddDesktopDataSource("testA1");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_DATASOURCE_TESTB))
            {
                DebugAddDesktopDataSource("testB");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_DATASOURCE_TESTB1))
            {
                DebugAddDesktopDataSource("testB1");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_DATASOURCE_TESTC))
            {
                DebugAddDesktopDataSource("testC");
            }

            if (Environment.GetCommandLineArgs().Contains(DebugArguments.ADD_DATASOURCE_TESTC1))
            {
                DebugAddDesktopDataSource("testC1");
            }
        }
    #endif
    }
    
    public ReadOnlyObservableCollection<DataSourceProxy> DataSources => _data;
    
    public ReactiveCommand<DataSourceProxy, Unit> RemoveDataSourceCommand { get; set; }

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
                // await _dataSourceService.CreateAndTryAddDataSource(result, FileSystemTypes.Directory);
                //await HandleNewDataSource(result, FileSystemTypes.Directory);
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
                    // await _dataSourceService.CreateAndTryAddDataSource(result, FileSystemTypes.File);
                    //await HandleNewDataSource(result, FileSystemTypes.File);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionMachineViewModel.AddDirectory");
        }
    }

    private async Task RemoveDataSource(DataSourceProxy dataSourceProxy)
    {
        await _dataSourceService.TryRemoveDataSource(dataSourceProxy.DataSource);
    }
}