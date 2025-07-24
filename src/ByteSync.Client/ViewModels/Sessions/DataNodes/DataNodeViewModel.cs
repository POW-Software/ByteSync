using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DynamicData;

namespace ByteSync.ViewModels.Sessions.DataNodes;

public class DataNodeViewModel : ActivatableViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IDataNodeService _dataNodeService;
    private readonly IDataNodeRepository _dataNodeRepository;
    
    private IBrush _currentMemberBackGround = null!;
    private IBrush _otherMemberBackGround = null!;

    public DataNodeSourcesViewModel SourcesViewModel { get; }
    public DataNodeHeaderViewModel HeaderViewModel { get; }
    public DataNodeStatusViewModel StatusViewModel { get; }

    public DataNodeViewModel()
    {

    }

    public DataNodeViewModel(SessionMember sessionMember, DataNode dataNode, bool isLocalMachine,
        DataNodeSourcesViewModel dataNodeSourcesViewModel, DataNodeHeaderViewModel dataNodeHeaderViewModel,
        DataNodeStatusViewModel dataNodeStatusViewModel,
        IThemeService themeService, IDataNodeService dataNodeService, IDataNodeRepository dataNodeRepository)
    {
        _themeService = themeService;
        _dataNodeService = dataNodeService;
        _dataNodeRepository = dataNodeRepository;
        
        SourcesViewModel = dataNodeSourcesViewModel;
        HeaderViewModel = dataNodeHeaderViewModel;
        StatusViewModel = dataNodeStatusViewModel;

        IsLocalMachine = isLocalMachine;
        JoinedSessionOn = sessionMember.JoinedSessionOn;
        OrderIndex = dataNode.OrderIndex;
        DataNode = dataNode;
        
        AddDataNodeCommand = ReactiveCommand.CreateFromTask(AddDataNode);
        
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
    
    public DataNode DataNode { get; }
    
    public ReactiveCommand<Unit, Unit> AddDataNodeCommand { get; }
}