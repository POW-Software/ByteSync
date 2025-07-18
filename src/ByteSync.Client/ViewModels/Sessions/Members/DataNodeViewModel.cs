using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business.Arguments;
using ByteSync.Business.DataSources;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Business.DataNodes;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Members;

public class DataNodeViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ILocalizationService _localizationService;
    private readonly IDataSourceService _dataSourceService;
    private readonly IDataSourceProxyFactory _dataSourceProxyFactory;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IFileDialogService _fileDialogService;
    private readonly IThemeService _themeService;
    private readonly ILogger<DataNodeViewModel> _logger;
    
    private ReadOnlyObservableCollection<DataSourceProxy> _dataSources;
    
    private IBrush? _currentMemberBackGround;
    private IBrush? _otherMemberBackGround;
    private IBrush? _unknownBadgeBrush;
    private IBrush? _currentMemberLetterBackGround;
    private IBrush? _otherMemberLetterBackGround;
    private IBrush? _currentMemberLetterBorder;
    private IBrush? _otherMemberLetterBorder;

    public DataNodeSourcesViewModel SourcesViewModel { get; }

    public DataNodeViewModel()
    {

    }

    public DataNodeViewModel(SessionMember sessionMember, DataNode dataNode, ISessionService sessionService, IDataSourceService dataSourceService,
        ILocalizationService localizationService, IEnvironmentService environmentService, IDataSourceProxyFactory dataSourceProxyFactory, 
        IDataSourceRepository dataSourceRepository, ISessionMemberRepository sessionMemberRepository, IFileDialogService fileDialogService,
        IThemeService themeService, ILogger<DataNodeViewModel> logger)
    {
        _sessionService = sessionService;
        _dataSourceService = dataSourceService;
        _localizationService = localizationService;
        _dataSourceProxyFactory = dataSourceProxyFactory;
        _dataSourceRepository = dataSourceRepository;
        _sessionMemberRepository = sessionMemberRepository;
        _fileDialogService = fileDialogService;
        _themeService = themeService;
        _logger = logger;


        
        // Initialisation du sous-view model dédié aux sources
        SourcesViewModel = new DataNodeSourcesViewModel(sessionMember, dataNode, IsLocalMachine, _sessionService, _dataSourceService,
            _dataSourceProxyFactory, _dataSourceRepository, _fileDialogService, _localizationService, environmentService);

        SessionMember = sessionMember;
        DataNode = dataNode;
        
        IsLocalMachine = sessionMember.ClientInstanceId.Equals(environmentService.ClientInstanceId);
        JoinedSessionOn = sessionMember.JoinedSessionOn;
        
        this.WhenAnyValue(x => x.IsLocalMachine)
            .Subscribe(_ => UpdateMachineDescription());
        
        this.WhenAnyValue(x => x.HasQuittedSessionAfterActivation)
            .Where(b => b)
            .Subscribe(_ => UpdateMachineDescription());

        ClientInstanceId = sessionMember.ClientInstanceId;

        RemoveDataSourceCommand = ReactiveCommand.CreateFromTask<DataSourceProxy>(RemoveDataSource);

        // // https://stackoverflow.com/questions/58479606/how-do-you-update-the-canexecute-value-after-the-reactivecommand-has-been-declar
        // // https://www.reactiveui.net/docs/handbook/commands/
        // var canRun = new BehaviorSubject<bool>(true);
        // AddDirectoryCommand = ReactiveCommand.CreateFromTask(AddDirectory, canRun);
        // AddFileCommand = ReactiveCommand.CreateFromTask(AddFiles, canRun);
        // Observable.Merge(AddDirectoryCommand.IsExecuting, AddFileCommand.IsExecuting)
        //     .Select(executing => !executing).Subscribe(canRun);

        var dataNodesObservable = _dataSourceRepository.ObservableCache.Connect()
            .Filter(ds => ds.DataNodeId == DataNode.NodeId)
            .Sort(SortExpressionComparer<DataSource>.Ascending(p => p.Code))
            .Transform(node => _dataSourceProxyFactory.CreateDataSourceProxy(node))
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _dataSources)
            .Subscribe();
        
        this.WhenActivated(disposables =>
        {
            SourcesViewModel.Activator.Activate()
                .DisposeWith(disposables);
            
            dataNodesObservable.DisposeWith(disposables);

            _sessionService.SessionStatusObservable.CombineLatest(_sessionService.RunSessionProfileInfoObservable)
                .DistinctUntilChanged()
                .Select(tuple => tuple.First == SessionStatus.Preparation && tuple.Second == null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsFileSystemSelectionEnabled)
                .DisposeWith(disposables);
            
            _sessionMemberRepository.Watch(sessionMember)
                .Subscribe(item =>
                {
                    UpdateStatus(item.Current.SessionMemberGeneralStatus);
                    PositionInList = item.Current.PositionInList;
                })
                .DisposeWith(disposables);

            Observable.FromEventPattern<PropertyChangedEventArgs>(_localizationService, nameof(_localizationService.PropertyChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(evt => OnLocaleChanged(evt.EventArgs))
                .DisposeWith(disposables);
            
            _themeService.SelectedTheme
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    InitializeBrushes();
                    SetMainGridBrush();
                })
                .DisposeWith(disposables);
        });

        InitializeBrushes();
        SetMainGridBrush();

        UpdateMachineDescription();
    }

    private void SetMainGridBrush()
    {
        MainGridBrush = IsLocalMachine switch
        {
            true => CurrentMemberBackGround,
            false => OtherMemberBackGround,
        };
        
        LetterBackBrush = IsLocalMachine switch
        {
            true => CurrentMemberLetterBackGround,
            false => OtherMemberLetterBackGround,
        };
        
        LetterBorderBrush = IsLocalMachine switch
        {
            true => CurrentMemberLetterBorder,
            false => OtherMemberLetterBorder,
        };
    }
    
    private void InitializeBrushes()
    {
        _currentMemberBackGround = _themeService.GetBrush("CurrentMemberBackGround");
        _otherMemberBackGround = _themeService.GetBrush("OtherMemberBackGround");
        _unknownBadgeBrush = _themeService.GetBrush("OtherMemberBackGround");
        
        _currentMemberLetterBackGround = _themeService.GetBrush("ConnectedMemberLetterBackGround");
        _otherMemberLetterBackGround = _themeService.GetBrush("DisabledMemberLetterBackGround");
        
        _currentMemberLetterBorder = _themeService.GetBrush("ConnectedMemberLetterBorder");
        _otherMemberLetterBorder = _themeService.GetBrush("DisabledMemberLetterBorder");
    }

    private IBrush CurrentMemberBackGround => _currentMemberBackGround;
    
    private IBrush OtherMemberBackGround => _otherMemberBackGround;
    
    private IBrush UnknownBadgeBrush => _unknownBadgeBrush;
    
    private IBrush CurrentMemberLetterBackGround => _currentMemberLetterBackGround;
    
    private IBrush OtherMemberLetterBackGround => _otherMemberLetterBackGround;
    
    private IBrush CurrentMemberLetterBorder => _currentMemberLetterBorder;
    
    private IBrush OtherMemberLetterBorder => _otherMemberLetterBorder;

    private void OnLocaleChanged(PropertyChangedEventArgs _)
    {
        UpdateMachineDescription();
        UpdateStatus(SessionMember.SessionMemberGeneralStatus);
    }

    public ReactiveCommand<DataSourceProxy, Unit> RemoveDataSourceCommand { get; set; }

    // public ReactiveCommand<Unit, Unit> AddDirectoryCommand { get; set; }
    //     
    // public ReactiveCommand<Unit, Unit> AddFileCommand { get; set; }

    [Reactive]
    public string ClientInstanceId { get; set; }

    [Reactive]
    public string MachineDescription { get; set; }
        
    [Reactive]
    public bool IsLocalMachine { get; set; }
    
    [Reactive]
    public IBrush MainGridBrush { get; set; }
    
    [Reactive]
    public IBrush LetterBackBrush { get; set; }
        
    [Reactive]
    public IBrush LetterBorderBrush { get; set; }
    
    [Reactive]
    public DateTimeOffset JoinedSessionOn { get; set; } 
    
    [Reactive]
    public bool HasQuittedSessionAfterActivation { get; set; }
    
    public extern bool IsFileSystemSelectionEnabled { [ObservableAsProperty] get; }

    [Reactive]
    public string Status { get; set; }
    
    [Reactive]
    public int PositionInList { get; set; }

    public ReadOnlyObservableCollection<DataSourceProxy> DataSources => _dataSources;
    
    internal SessionMember SessionMember { get; private set; }
    
    internal DataNode DataNode { get; private set; }

    private async Task RemoveDataSource(DataSourceProxy dataSource)
    {
        await _dataSourceService.TryRemoveDataSource(dataSource.DataSource);
    }

    private async Task AddDirectory()
    {
        try
        {
            var result = await _fileDialogService.ShowOpenFolderDialogAsync(Resources.SessionMachineView_SelectDirectory);

            if (result != null && Directory.Exists(result))
            {
                await _dataSourceService.CreateAndTryAddDataSource(result, FileSystemTypes.Directory, DataNode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionMachineViewModel.AddDirectory");
        }
    }

    private async Task AddFiles()
    {
        var result = await _fileDialogService.ShowOpenFileDialogAsync(Resources.SessionMachineView_SelectFiles, true);

        if (result != null)
        {
            foreach (var fileName in result)
            {
                await _dataSourceService.CreateAndTryAddDataSource(fileName, FileSystemTypes.File, DataNode);
            }
        }
    }

    private void UpdateMachineDescription()
    {
        string machineDescription;
        if (IsLocalMachine)
        {
            machineDescription = $"{_localizationService[nameof(Resources.SessionMachineView_ThisComputer)]} " +
                                 $"({SessionMember.MachineName}, {SessionMember.IpAddress})";

#if DEBUG
            machineDescription += " - PID:" + Process.GetCurrentProcess().Id;
#endif
        }
        else
        {
            machineDescription = $"{SessionMember.MachineName}, {SessionMember.IpAddress}";
        }
        
        if (HasQuittedSessionAfterActivation)
        {
            machineDescription += " - " + _localizationService[nameof(Resources.SessionMachineView_LeftSession)];
        }

        MachineDescription = machineDescription;
    }

    private void UpdateStatus(SessionMemberGeneralStatus localInventoryStatus)
    {
        switch (localInventoryStatus)
        {
            case SessionMemberGeneralStatus.InventoryWaitingForStart:
                Status = Resources.SessionMachine_Status_WaitingForStart;
                break;
            case SessionMemberGeneralStatus.InventoryRunningIdentification:
                Status = Resources.SessionMachine_Status_RunningIdentification;
                break;
            case SessionMemberGeneralStatus.InventoryWaitingForAnalysis:
                Status = Resources.SessionMachine_Status_WaitingForAnalysis;
                break;
            case SessionMemberGeneralStatus.InventoryRunningAnalysis:
                Status = Resources.SessionMachine_Status_RunningAnalysis;
                break;
            case SessionMemberGeneralStatus.InventoryCancelled:
                Status = Resources.SessionMachine_Status_InventoryCancelled;
                break;
            case SessionMemberGeneralStatus.InventoryError:
                Status = Resources.SessionMachine_Status_InventoryError;
                break;
            case SessionMemberGeneralStatus.InventoryFinished:
                Status = Resources.SessionMachine_Status_Finished;
                break;
            case SessionMemberGeneralStatus.SynchronizationRunning:
                Status = Resources.SessionMachine_Status_SynchronizationRunning;
                break;
            case SessionMemberGeneralStatus.SynchronizationSourceActionsInitiated:
                Status = Resources.SessionMachine_Status_SynchronizationSourceActionsInitiated;
                break;
            case SessionMemberGeneralStatus.SynchronizationError:
                Status = Resources.SessionMachine_Status_SynchronizationError;
                break;
            case SessionMemberGeneralStatus.SynchronizationFinished:
                Status = Resources.SessionMachine_Status_SynchronizationFinished;
                break;
            default:
                Status = Resources.SessionMachine_Status_UnknownStatus;
                break;
        }
    }
}