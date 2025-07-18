using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.DataNodes;
using ByteSync.Interfaces.Controls.Themes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Members;

public class DataNodeViewModel : ActivatableViewModelBase
{
    private readonly IThemeService _themeService;
    
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
        IThemeService themeService)
    {
        _themeService = themeService;
        
        SourcesViewModel = dataNodeSourcesViewModel;
        HeaderViewModel = dataNodeHeaderViewModel;
        StatusViewModel = dataNodeStatusViewModel;

        IsLocalMachine = isLocalMachine;
        JoinedSessionOn = sessionMember.JoinedSessionOn;
        
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
        
    [Reactive]
    public bool IsLocalMachine { get; set; }
    
    [Reactive]
    public IBrush MainGridBrush { get; set; } = null!;
        
    [Reactive]
    public DateTimeOffset JoinedSessionOn { get; set; } 
}