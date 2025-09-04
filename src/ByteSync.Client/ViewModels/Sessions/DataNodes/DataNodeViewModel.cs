using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DynamicData;

namespace ByteSync.ViewModels.Sessions.DataNodes;

public class DataNodeViewModel : ActivatableViewModelBase
{
    private readonly IThemeService _themeService = null!;
    private readonly IDataNodeService _dataNodeService = null!;
    private readonly IDataNodeRepository _dataNodeRepository = null!;
    private readonly ISessionService _sessionService = null!;
    
    private IBrush _currentMemberBackGround = null!;
    private IBrush _otherMemberBackGround = null!;

    public DataNodeSourcesViewModel SourcesViewModel { get; } = null!;
    public DataNodeHeaderViewModel HeaderViewModel { get; } = null!;
    public DataNodeStatusViewModel StatusViewModel { get; } = null!;

    public DataNodeViewModel()
    {

    }

    public DataNodeViewModel(SessionMember sessionMember, DataNode dataNode, bool isLocalMachine,
        DataNodeSourcesViewModel dataNodeSourcesViewModel, DataNodeHeaderViewModel dataNodeHeaderViewModel,
        DataNodeStatusViewModel dataNodeStatusViewModel,
        IThemeService themeService, IDataNodeService dataNodeService, IDataNodeRepository dataNodeRepository,
        ISessionService sessionService)
    {
        _themeService = themeService;
        _dataNodeService = dataNodeService;
        _dataNodeRepository = dataNodeRepository;
        _sessionService = sessionService;
        
        SourcesViewModel = dataNodeSourcesViewModel;
        HeaderViewModel = dataNodeHeaderViewModel;
        StatusViewModel = dataNodeStatusViewModel;

        IsLocalMachine = isLocalMachine;
        JoinedSessionOn = sessionMember.JoinedSessionOn;
        OrderIndex = dataNode.OrderIndex;
        DataNode = dataNode;
        
        // Create command with canExecute based on session status
        var canAddDataNode = _sessionService.SessionStatusObservable
            .Select(status => status == SessionStatus.Preparation)
            .CombineLatest(this.WhenAnyValue(x => x.IsLocalMachine, x => x.IsLastDataNode),
                (isSessionInPreparation, localAndLast) => 
                    isSessionInPreparation && localAndLast.Item1 && localAndLast.Item2)
            .ObserveOn(RxApp.MainThreadScheduler);
        
        AddDataNodeCommand = ReactiveCommand.CreateFromTask(AddDataNode, canAddDataNode);
        
        this.WhenActivated(disposables =>
        {
            SourcesViewModel.Activator.Activate()
                .DisposeWith(disposables);

            HeaderViewModel.Activator.Activate()
                .DisposeWith(disposables);
            
            StatusViewModel.Activator.Activate()
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
            
            _dataNodeRepository.ObservableCache.Connect()
                .WhereReasonsAre(ChangeReason.Add, ChangeReason.Remove)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateIsLastDataNode())
                .DisposeWith(disposables);
            
            UpdateIsLastDataNode();
            
            this.WhenAnyValue(x => x.IsLocalMachine, x => x.IsLastDataNode)
                .Select(tuple => tuple.Item1 && tuple.Item2)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.ShowAddButton)
                .DisposeWith(disposables);
        });

        InitializeBrushes();
        SetMainGridBrush();
    }

    private void SetMainGridBrush()
    {
        MainGridBrush = IsLocalMachine switch
        {
            true => CurrentMemberBackGround,
            false => OtherMemberBackGround,
        };
    }
    
    private void InitializeBrushes()
    {
        _currentMemberBackGround = _themeService.GetBrush("CurrentMemberBackGround")!;
        _otherMemberBackGround = _themeService.GetBrush("OtherMemberBackGround")!;
    }
    
    private IBrush CurrentMemberBackGround => _currentMemberBackGround;

    private IBrush OtherMemberBackGround => _otherMemberBackGround;
    
    private void UpdateIsLastDataNode()
    {
        if (!IsLocalMachine)
        {
            IsLastDataNode = false;
            return;
        }
        
        var currentMemberDataNodes = _dataNodeRepository.SortedCurrentMemberDataNodes;
        if (currentMemberDataNodes.Count == 0)
        {
            IsLastDataNode = false;
            return;
        }
        
        var lastDataNode = currentMemberDataNodes.Last();
        IsLastDataNode = DataNode.Id == lastDataNode.Id;
    }
    
    private async Task AddDataNode()
    {
        await _dataNodeService.CreateAndTryAddDataNode();
    }
        
    [Reactive]
    public bool IsLocalMachine { get; set; }
    
    [Reactive]
    public IBrush MainGridBrush { get; set; } = null!;
        
    [Reactive]
    public DateTimeOffset JoinedSessionOn { get; set; }
    
    [Reactive]
    public int OrderIndex { get; set; }
    
    [Reactive]
    public bool IsLastDataNode { get; set; }
    
    public extern bool ShowAddButton { [ObservableAsProperty] get; }
    
    public DataNode DataNode { get; } = null!;

    public ReactiveCommand<Unit, Unit> AddDataNodeCommand { get; } = null!;
}