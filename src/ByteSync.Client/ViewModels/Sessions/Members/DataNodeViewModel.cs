using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.DataNodes;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Sessions.Members;

public class DataNodeViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService;
    // _localizationService removed: header view model now manages localization
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IThemeService _themeService;
    private readonly ILogger<DataNodeViewModel> _logger;
    
    
    private IBrush _currentMemberBackGround = null!;
    private IBrush _otherMemberBackGround = null!;
    // Letter brushes moved to header view model - plus utilisés ici

    public DataNodeSourcesViewModel SourcesViewModel { get; }
    public DataNodeHeaderViewModel HeaderViewModel { get; }

    public DataNodeViewModel()
    {

    }

    public DataNodeViewModel(SessionMember sessionMember, DataNode dataNode, bool isLocalMachine,
        DataNodeSourcesViewModel dataNodeSourcesViewModel, DataNodeHeaderViewModel dataNodeHeaderViewModel,
        ISessionService sessionService, ISessionMemberRepository sessionMemberRepository,
        IThemeService themeService, ILogger<DataNodeViewModel> logger)
    {
        _sessionService = sessionService;
        _sessionMemberRepository = sessionMemberRepository;
        _themeService = themeService;
        _logger = logger;
        
        SourcesViewModel = dataNodeSourcesViewModel;
        HeaderViewModel = dataNodeHeaderViewModel;

        SessionMember = sessionMember;
        DataNode = dataNode;

        IsLocalMachine = isLocalMachine;
        JoinedSessionOn = sessionMember.JoinedSessionOn;
        
        // HeaderViewModel handles machine description refresh.
        
        // Header properties managed by header view model.
        
        // Letter brushes handled by header view model.
        
        this.WhenActivated(disposables =>
        {
            SourcesViewModel.Activator.Activate()
                .DisposeWith(disposables);

            HeaderViewModel.Activator.Activate()
                .DisposeWith(disposables);

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
                })
                .DisposeWith(disposables);

            // Localization updates handled by header view model.
            
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
        // Machine description handled by header view model.
    }

    private void SetMainGridBrush()
    {
        MainGridBrush = IsLocalMachine switch
        {
            true => CurrentMemberBackGround,
            false => OtherMemberBackGround,
        };
        
        // Letter brushes handled by header view model.
    }
    
    private void InitializeBrushes()
    {
        _currentMemberBackGround = _themeService.GetBrush("CurrentMemberBackGround")!;
        _otherMemberBackGround = _themeService.GetBrush("OtherMemberBackGround")!;
        
        // Header view model handles letter brushes, donc aucune initialisation ici.
    }

    // Letter brushes moved to header view model
    
    private IBrush CurrentMemberBackGround => _currentMemberBackGround;

    private IBrush OtherMemberBackGround => _otherMemberBackGround;

    // Letter background brushes handled by header view model
    
    // Localization updates handled by header view model.

    // Header-related reactive properties removed from this ViewModel.
        
    [Reactive]
    public bool IsLocalMachine { get; set; }
    
    [Reactive]
    public IBrush MainGridBrush { get; set; }
    
    // Letter brushes moved to header view model
        
    [Reactive]
    public DateTimeOffset JoinedSessionOn { get; set; } 
    
    // Property handled by HeaderViewModel
    
    public extern bool IsFileSystemSelectionEnabled { [ObservableAsProperty] get; }

    [Reactive]
    public string Status { get; set; }
    
    // Header-related reactive properties removed from this ViewModel.
    
    internal SessionMember SessionMember { get; private set; }
    
    internal DataNode DataNode { get; private set; }

    // Machine description handled by header view model.

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