using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Assets.Resources;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.DataNodes;

public class DataNodeSourcesViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly IDataSourceService _dataSourceService;
    private readonly IDataSourceProxyFactory _dataSourceProxyFactory;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IFileDialogService _fileDialogService;
    private readonly DataNode _dataNode;

    private ReadOnlyObservableCollection<DataSourceProxy> _dataSources;

    public DataNodeSourcesViewModel(DataNode dataNode,
        bool isLocalMachine,
        ISessionService sessionService,
        IDataSourceService dataSourceService,
        IDataSourceProxyFactory dataSourceProxyFactory,
        IDataSourceRepository dataSourceRepository,
        IFileDialogService fileDialogService)
    {
        _dataNode = dataNode;
        _sessionService = sessionService;
        _dataSourceService = dataSourceService;
        _dataSourceProxyFactory = dataSourceProxyFactory;
        _dataSourceRepository = dataSourceRepository;
        _fileDialogService = fileDialogService;

        IsLocalMachine = isLocalMachine;
        
        var canRun = new BehaviorSubject<bool>(true);
        AddDirectoryCommand = ReactiveCommand.CreateFromTask(AddDirectory, canRun);
        AddFileCommand = ReactiveCommand.CreateFromTask(AddFiles, canRun);
        RemoveDataSourceCommand = ReactiveCommand.CreateFromTask<DataSourceProxy>(RemoveDataSource);

        Observable.Merge(AddDirectoryCommand.IsExecuting, AddFileCommand.IsExecuting)
            .Select(executing => !executing)
            .Subscribe(canRun);
        
        var dataNodesObservable = _dataSourceRepository.ObservableCache.Connect()
            .Filter(ds => ds.DataNodeId == _dataNode.Id)
            .Sort(SortExpressionComparer<DataSource>.Ascending(p => p.Code))
            .Transform(node => _dataSourceProxyFactory.CreateDataSourceProxy(node))
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _dataSources)
            .Subscribe();
        
        this.WhenActivated(disposables =>
        {
            dataNodesObservable.DisposeWith(disposables);

            _sessionService.SessionStatusObservable.CombineLatest(_sessionService.RunSessionProfileInfoObservable)
                .DistinctUntilChanged()
                .Select(tuple => tuple.First == SessionStatus.Preparation && tuple.Second == null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsFileSystemSelectionEnabled)
                .DisposeWith(disposables);
        });
    }

    public ReadOnlyObservableCollection<DataSourceProxy> DataSources => _dataSources;

    public ReactiveCommand<DataSourceProxy, Unit> RemoveDataSourceCommand { get; }

    public ReactiveCommand<Unit, Unit> AddDirectoryCommand { get; }

    public ReactiveCommand<Unit, Unit> AddFileCommand { get; }

    [Reactive]
    public bool IsLocalMachine { get; set; }

    public extern bool IsFileSystemSelectionEnabled { [ObservableAsProperty] get; }

    private async Task RemoveDataSource(DataSourceProxy dataSource)
    {
        await _dataSourceService.TryRemoveDataSource(dataSource.DataSource);
    }

    private async Task AddDirectory()
    {
        var result = await _fileDialogService.ShowOpenFolderDialogAsync(Resources.SessionMachineView_SelectDirectory);

        if (result != null && Directory.Exists(result))
        {
            await _dataSourceService.CreateAndTryAddDataSource(result, FileSystemTypes.Directory, _dataNode);
        }
    }

    private async Task AddFiles()
    {
        var results = await _fileDialogService.ShowOpenFileDialogAsync(Resources.SessionMachineView_SelectFiles, true);

        if (results != null)
        {
            foreach (var fileName in results)
            {
                await _dataSourceService.CreateAndTryAddDataSource(fileName, FileSystemTypes.File, _dataNode);
            }
        }
    }
} 