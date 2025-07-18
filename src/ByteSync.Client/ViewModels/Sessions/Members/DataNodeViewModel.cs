using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.DataNodes;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Members;

public class DataNodeViewModel : ActivatableViewModelBase
{
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IThemeService _themeService;
    
    private IBrush _currentMemberBackGround = null!;
    private IBrush _otherMemberBackGround = null!;

    public DataNodeSourcesViewModel SourcesViewModel { get; }
    public DataNodeHeaderViewModel HeaderViewModel { get; }

    public DataNodeViewModel()
    {

    }

    public DataNodeViewModel(SessionMember sessionMember, DataNode dataNode, bool isLocalMachine,
        DataNodeSourcesViewModel dataNodeSourcesViewModel, DataNodeHeaderViewModel dataNodeHeaderViewModel,
        ISessionMemberRepository sessionMemberRepository, IThemeService themeService)
    {
        _sessionMemberRepository = sessionMemberRepository;
        _themeService = themeService;
        
        SourcesViewModel = dataNodeSourcesViewModel;
        HeaderViewModel = dataNodeHeaderViewModel;

        SessionMember = sessionMember;
        DataNode = dataNode;

        IsLocalMachine = isLocalMachine;
        JoinedSessionOn = sessionMember.JoinedSessionOn;
        
        this.WhenActivated(disposables =>
        {
            SourcesViewModel.Activator.Activate()
                .DisposeWith(disposables);

            HeaderViewModel.Activator.Activate()
                .DisposeWith(disposables);
            
            _sessionMemberRepository.Watch(sessionMember)
                .Subscribe(item =>
                {
                    UpdateStatus(item.Current.SessionMemberGeneralStatus);
                })
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

    [Reactive]
    public string Status { get; set; } = null!;
    
    internal SessionMember SessionMember { get; private set; }
    
    internal DataNode DataNode { get; private set; }

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